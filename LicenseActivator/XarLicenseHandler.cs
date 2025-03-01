using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;

namespace LicenseActivator;

public enum ActivationResultCode
{
    Success = 0,
    TcXaeShellNotFound = 1,
    MainWindowNotFound = 2,
    ManageLicenseButtonNotFound = 3,
    ExtendTrialLicensesButtonNotFound = 4,
    ExtendSecurityCodeDialogNotFound = 5,
    LicenseInformationFieldNotFound = 6,
    LicenseStringNotValid = 7,
    LicenseInputFieldNotFound = 8,
    SecurityCodeDialogOkButtonNotFound = 9,
    SecurityCodeDialogOkButtonDisabled = 10,
    TcXaeShellDialogNotFound = 11,
    TcXaeShellDialogOkButtonNotFound = 12,
    IssueTimeElementNotFound = 13,
    IssueTimeElementParseFailed = 14,
    ExpireTimeElementNotFound = 15,
    ExpireTimeElementParseFailed = 16,
}

public interface IXarLicenseHandler
{
    ActivationResultCode Activate();
    
    DateTimeOffset ExpireTime { get; }
    DateTimeOffset IssueTime { get; }
}

public class XarLicenseHandler: IXarLicenseHandler
{
    private const string TcXaeShellPath = @"C:\Program Files (x86)\Beckhoff\TcXaeShell\Common7\IDE\TcXaeShell.exe";
    private const string ProjectPath = @"C:\Software\TwinCat\TwinCAT L1.sln";
    private const string ManageLicenseButtonName = "License";
    private const string ExtendTrialLicenseButtonName = "7 Days Trial License...";
    private const string EnterSecurityCodeDialogName = "Enter Security Code";
    private const string LicenseInformationFieldId = "3157";
    private const string LicenseInputFieldId = "1544";
    private const string OkButtonName = "OK";
    private const string TcXaeShellDialogName = "TcXaeShell";

    public DateTimeOffset ExpireTime { get; private set; } = DateTimeOffset.MinValue;
    public DateTimeOffset IssueTime { get; private set; } = DateTimeOffset.MinValue;
    
    public ActivationResultCode Activate()
    {
        // TcXaeShell starten
        var process = StartOrAttachToTcXaeShell(TcXaeShellPath, ProjectPath);
        if (process == null)
            return ActivationResultCode.TcXaeShellNotFound;

        // Warten, bis das UI geladen ist
        using var automation = new UIA3Automation();
        var app = FlaUI.Core.Application.Attach(process);
        var window = WaitForMainWindow(app, automation);
        if (window == null)
            return ActivationResultCode.MainWindowNotFound;

        window.Focus();
        window.Patterns.Window.Pattern.SetWindowVisualState(FlaUI.Core.Definitions.WindowVisualState.Normal);
        window.SetForeground();

        // Schaltfläche suchen und klicken
        var manageLicenseButton = window.FindFirstDescendant(cf => cf.ByName(ManageLicenseButtonName))?.AsButton();
        if (manageLicenseButton == null) 
            return ActivationResultCode.ManageLicenseButtonNotFound;
        manageLicenseButton.DoubleClick();
            
        // Schaltfläche zur Verlängerung der Lizenzen suchen und klicken
        var extendLicenseButton = window.FindFirstDescendant(cf => cf.ByName(ExtendTrialLicenseButtonName))?.AsButton();
        if (extendLicenseButton == null)
            return ActivationResultCode.ExtendTrialLicensesButtonNotFound;
        extendLicenseButton.Click();

        // Lizenzdialog suchen
        var securityCodeDialog = window.FindFirstDescendant(cf => cf.ByName(EnterSecurityCodeDialogName))?.AsWindow();
        if (securityCodeDialog == null)
            return ActivationResultCode.ExtendSecurityCodeDialogNotFound;
        
        // Lizenz auslesen und schreiben
        var licenseInformationField = securityCodeDialog.FindFirstDescendant(cf => cf.ByAutomationId(LicenseInformationFieldId))?.AsTextBox();
        if(licenseInformationField == null) 
            return ActivationResultCode.LicenseInformationFieldNotFound;

        var license = licenseInformationField.Patterns.Value.Pattern.Value.Value;
        if (!Regex.IsMatch(license, @"^[A-Za-z0-9]{5}$"))
            return ActivationResultCode.LicenseStringNotValid;
        
        var licenseInputField = securityCodeDialog.FindFirstDescendant(cf => cf.ByAutomationId(LicenseInputFieldId))?.AsTextBox();
        if (licenseInputField == null)
            return ActivationResultCode.LicenseInputFieldNotFound;
        
        licenseInputField.Text = license;
        
        // Dialog bestätigen
        var okButton = securityCodeDialog.FindFirstDescendant(cf => cf.ByName(OkButtonName))?.AsButton();
        if (okButton == null)
            return ActivationResultCode.SecurityCodeDialogOkButtonNotFound;
        if(!okButton.IsEnabled)
            return ActivationResultCode.SecurityCodeDialogOkButtonDisabled;
        okButton.Click();
        
        // TcXaeShell Dialog suchen
        var tcXaeShellDialog = window.FindFirstDescendant(cf => cf.ByName(TcXaeShellDialogName))?.AsWindow();
        if (tcXaeShellDialog == null)
            return ActivationResultCode.TcXaeShellDialogNotFound;
        
        // Pfad zum Lizenzfile suchen
        var licenseFileLocation = Regex.Match(tcXaeShellDialog.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text))?.AsLabel()?.Text ?? string.Empty, @"'([A-Z]:\\[^']+)'").Groups[1].Value;
        
        // Ok Button suchen und klicken
        var tcXaeShellOkButton = tcXaeShellDialog.FindFirstDescendant(cf => cf.ByName(OkButtonName))?.AsButton();
        if (tcXaeShellOkButton == null)
            return ActivationResultCode.TcXaeShellDialogOkButtonNotFound;
        tcXaeShellOkButton.Click();
        
        // Datei am in licenseFileLocation enthaltenen Pfad öffnen und als xml interpretieren
        var licenseFile = XDocument.Load(licenseFileLocation);
        var issueTimeElement = licenseFile.Descendants("IssueTime").FirstOrDefault();
        if (issueTimeElement == null)
            return ActivationResultCode.IssueTimeElementNotFound;
        if(!DateTimeOffset.TryParse(issueTimeElement.Value, out var issueTime))
            return ActivationResultCode.IssueTimeElementParseFailed;
        IssueTime = issueTime;
        
        var expireTimeElement = licenseFile.Descendants("ExpireTime").FirstOrDefault();
        if(expireTimeElement == null)
            return ActivationResultCode.ExpireTimeElementNotFound;
        if(!DateTimeOffset.TryParse(expireTimeElement.Value, out var expireTime))
            return ActivationResultCode.ExpireTimeElementParseFailed;
        ExpireTime = expireTime;
    
        Console.WriteLine("License successful extended. IssueTime: {0}, ExpireTime: {1}", issueTime, expireTime);
        return ActivationResultCode.Success;
    }
    
    private static Process? StartOrAttachToTcXaeShell(string exePath, string projectPath)
    {
        var existingProcess = Process.GetProcessesByName("TcXaeShell").FirstOrDefault();
        if (existingProcess != null)
        {
            Console.WriteLine("TcXaeShell already running.");
            return existingProcess;
        }
    
        var startInfo = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"\"{projectPath}\"",
            UseShellExecute = true
        };
    
        try
        {
            return Process.Start(startInfo);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on start of Shell: {ex.Message}");
            return null;
        }
    }

    private static Window? WaitForMainWindow(FlaUI.Core.Application app, UIA3Automation automation)
    {
        var timeout = TimeSpan.FromSeconds(20);
        
        var window = app.GetMainWindow(automation, timeout);
        if (window == null)
            return window;
        window.WaitUntilClickable(timeout);
        window.WaitUntilEnabled(timeout);
        return window;
    }
}