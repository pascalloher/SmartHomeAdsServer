using L1AdsServer.Core;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace L1AdsServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DimmerController : ControllerBase
{
    private readonly IDimmerControl _dimmerControl;

    public DimmerController(IDimmerControl dimmerControl)
    {
        _dimmerControl = dimmerControl;
    }

    // GET api/<Dimmer>/5
    [HttpGet("{id}")]
    public async Task<uint> Get(int id, CancellationToken token)
    {
        return await _dimmerControl.GetAsync(id, token);
    }

    // PUT api/dimmer/on/OG3
    [HttpPut("on/{id}")]
    public async Task On(DimmerId id, CancellationToken token)
    {
        await _dimmerControl.OnAsync(id, token);
    }

    // PUT api/dimmer/off/OG5
    [HttpPut("off/{id}")]
    public async Task Off(DimmerId id, CancellationToken token)
    {
        await _dimmerControl.OffAsync(id, token);
    }

    // PUT api/dimmer/set/OG5
    [HttpPut("set/{id}")]
    public async Task Set(DimmerId id, [FromBody] byte value, CancellationToken token)
    {
        await _dimmerControl.SetAsync(id, (ushort)(value * 128), token);
    }
}
