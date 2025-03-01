using L1AdsServer.Core.Controls;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace L1AdsServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SwitchController : ControllerBase
{
    private readonly ISwitchControl _SwitchControl;

    public SwitchController(ISwitchControl SwitchControl)
    {
        _SwitchControl = SwitchControl;
    }

    // PUT api/Switch/on/Og5
    [HttpPut("on/{id}")]
    public async Task On(SwitchId id, CancellationToken token)
    {
        await _SwitchControl.OnAsync(id, token);
    }

    // PUT api/Switch/off/Og5
    [HttpPut("off/{id}")]
    public async Task Off(SwitchId id, CancellationToken token)
    {
        await _SwitchControl.OffAsync(id, token);
    }
}
