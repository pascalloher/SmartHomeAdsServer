using LicenseActivator;
using Microsoft.AspNetCore.Mvc;

namespace L1AdsServer.Controllers;

public record ActivationResult(ActivationResultCode ResultCode, DateTimeOffset? IssueTime, DateTimeOffset? ExpireTime); 

[ApiController]
[Route("api/[controller]")]
public class XarController: ControllerBase
{
    private readonly IXarLicenseHandler _xarLicenseHandler;

    public XarController(IXarLicenseHandler xarLicenseHandler)
    {
        _xarLicenseHandler = xarLicenseHandler;
    }
    
    // POST api/xar/activateLicense
    [HttpPost("activateLicense")]
    public ActivationResult ActivateLicense()
    {
        try
        {
            var result = _xarLicenseHandler.Activate();
            if (result == ActivationResultCode.Success)
                return new ActivationResult(result, _xarLicenseHandler.IssueTime, _xarLicenseHandler.ExpireTime);
        }
        catch(Exception _)
        {
            // Nichts zu tun...
        }
        return new ActivationResult(ActivationResultCode.Exception, null, null);
    }
}
