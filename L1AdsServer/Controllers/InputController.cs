using L1AdsServer.Core.Controls;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace L1AdsServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InputController : ControllerBase
{
    private readonly ILogger<LedControl> _logger;
    private readonly IInputControl _inputControl;

    public InputController(ILogger<LedControl> logger, IInputControl InputControl)
    {
        _logger = logger;
        _inputControl = InputControl;
    }

    // POST api/input/in/UV_EG1
    [HttpPost("in/{id}")]
    public async Task<bool> In(InputId id, [FromBody] DeviceInfo deviceInfo, CancellationToken token)
    {
        _logger.LogInformation(new EventId(1378499020), $"{DateTime.Now} GetInput for {id}, DeviceName: {deviceInfo.DeviceName}");
        return await _inputControl.GetAsync(id, deviceInfo, token);
    }
}
