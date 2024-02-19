using L1AdsServer.Core;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace L1AdsServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LedController : ControllerBase
{
    private readonly ILedControl _ledControl;

    public LedController(ILedControl LedControl)
    {
        _ledControl = LedControl;
    }

    // PUT api/led/on/Og5
    [HttpPut("on/{id}")]
    public async Task On(LedId id, CancellationToken token)
    {
        await _ledControl.OnAsync(id, token);
    }

    // PUT api/led/off/Og5
    [HttpPut("off/{id}")]
    public async Task Off(LedId id, CancellationToken token)
    {
        await _ledControl.OffAsync(id, token);
    }

    // PUT api/led/level/Og5
    [HttpPut("level/{id}")]
    public async Task Level(LedId id, [FromForm] int brightness, CancellationToken token)
    {
        await _ledControl.LevelAsync(id, brightness / 255.0, token);
    }

    // PUT api/led/temperature/Og5
    [HttpPut("temperature/{id}")]
    public async Task Temperature(LedId id, [FromForm] int colorTemp, CancellationToken token)
    {
        // Temperatur ranges from 153 to 500
        await _ledControl.TemperatureAsync(id, (colorTemp - 153) / (500.0 - 153.0), token);
    }
}
