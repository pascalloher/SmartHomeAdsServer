using L1AdsServer.Core;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace L1AdsServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DoorController : ControllerBase
{
    private readonly IDoorControl _doorControl;

    public DoorController(IDoorControl doorControl)
    {
        _doorControl = doorControl;
    }

    // PUT api/door/open/Ga1
    [HttpPut("open/{id}")]
    public async Task Open(DoorId id, CancellationToken token)
    {
        await _doorControl.OpenAsync(id, token);
    }

    // PUT api/door/close/OG5
    [HttpPut("close/{id}")]
    public async Task Close(DoorId id, CancellationToken token)
    {
        await _doorControl.CloseAsync(id, token);
    }

    // PUT api/door/stop/OG5
    [HttpPut("stop/{id}")]
    public async Task Stop(DoorId id, CancellationToken token)
    {
        await _doorControl.StopAsync(id, token);
    }
}
