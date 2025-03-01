using L1AdsServer.Core.Controls;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace L1AdsServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlindController : ControllerBase
{
    private readonly ILogger<BlindController> _logger;
    private readonly IBlindControl _blindControl;

    public BlindController(ILogger<BlindController> logger, IBlindControl blindControl)
    {
        _logger = logger;
        _blindControl = blindControl;
    }

    // PUT api/blind/open/OG3
    [HttpPut("open/{id}")]
    public async Task Open(BlindId id, CancellationToken token)
    {
        _logger.LogInformation(new EventId(702757139), "api/blind/open/{id} called", id);
        await _blindControl.OpenAsync(id, token);
    }

    // PUT api/blind/close/OG5
    [HttpPut("close/{id}")]
    public async Task Close(BlindId id, CancellationToken token)
    {
        _logger.LogInformation(new EventId(424141360), "api/blind/close/{id} called", id);
        await _blindControl.CloseAsync(id, token);
    }

    // PUT api/blind/stop/OG5
    [HttpPut("stop/{id}")]
    public async Task Stop(BlindId id, CancellationToken token)
    {
        _logger.LogInformation(new EventId(705612599), "api/blind/stop/{id} called", id);
        await _blindControl.StopAsync(id, token);
    }
}
