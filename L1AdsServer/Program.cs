using System.Reflection;
using L1AdsServer.Configuration;
using L1AdsServer.Core.Common;
using L1AdsServer.Core.Controls;
using L1AdsServer.Core.Plc;
using LicenseActivator;
using Microsoft.Extensions.Hosting.WindowsServices;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Check if the app is running as a Windows Service
if (WindowsServiceHelpers.IsWindowsService())
{
    builder.Host.UseWindowsService();
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({EventId}) {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: @"C:\ProgramData\L1\L1AdsServer\logs\log-.log",
        rollingInterval: RollingInterval.Day,
        fileSizeLimitBytes: 1000_000_000,
        retainedFileCountLimit: 10,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({EventId}) {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information($@"
****************************************************************
** Start L1AdsServer (Mode: {(WindowsServiceHelpers.IsWindowsService() ? "Service" : "Application")})
**
** Version: {(Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown")} (Build: {new FileInfo(Assembly.GetExecutingAssembly().Location).LastWriteTime:yyyy-MM-dd HH:mm:ss})
****************************************************************
");

    builder.Host.UseSerilog();

    var configuration = builder.Configuration;
    var homeAssistantConfig = configuration.GetSection("HomeAssistant").Get<HomeAssistantConfig>();

    builder.Services.AddHttpClient(nameof(DataControl), client =>
    {
        client.BaseAddress = homeAssistantConfig?.Uri;
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + homeAssistantConfig?.BearerToken);
    });

    // Add CORS
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(b =>
        {
            b.AllowAnyOrigin();
        });
    });

    // Add services to the container.
    builder.Services.AddSingleton<IHeartbeatMonitor, HeartbeatMonitor>();
    builder.Services.AddSingleton<IDataExtractor, DataExtractor>();

    builder.Services.AddSingleton<IBlindControl, BlindControl>();
    builder.Services.AddSingleton<IDimmerControl, DimmerControl>();
    builder.Services.AddSingleton<IDoorControl, DoorControl>();
    builder.Services.AddSingleton<IInputControl, InputControl>();
    builder.Services.AddSingleton<ILedControl, LedControl>();
    builder.Services.AddSingleton<ISwitchControl, SwitchControl>();
    builder.Services.AddSingleton<IDataControl, DataControl>();
    builder.Services.AddSingleton<IXarLicenseHandler, XarLicenseHandler>();

    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    var app = builder.Build();

    //app.UseHttpLogging();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseCors();

    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application start-up failed");
}
finally
{
    Log.CloseAndFlush();
}
