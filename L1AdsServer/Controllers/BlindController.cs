using L1AdsServer.Core;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace L1AdsServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BlindController : ControllerBase
{
    private readonly IBlindControl _blindControl;

    public BlindController(IBlindControl blindControl)
    {
        _blindControl = blindControl;
    }

    // PUT api/blind/open/OG3
    [HttpPut("open/{id}")]
    public async Task Open(BlindId id, CancellationToken token)
    {
        await _blindControl.OpenAsync(id, token);
    }

    // PUT api/blind/close/OG5
    [HttpPut("close/{id}")]
    public async Task Close(BlindId id, CancellationToken token)
    {
        await _blindControl.CloseAsync(id, token);
    }

    // PUT api/blind/stop/OG5
    [HttpPut("stop/{id}")]
    public async Task Stop(BlindId id, CancellationToken token)
    {
        await _blindControl.StopAsync(id, token);
    }
}
