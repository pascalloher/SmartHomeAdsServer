using L1AdsServer.Core;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace L1AdsServer.Controllers;

public class DataDescription(string plcName, string type)
{
    public string PlcName { get; } = plcName;
    public string Type { get; } = type;
    public string HaName { get; } = "";
    public bool RegisterChangeDetection { get; } = false;
}

[ApiController]
[Route("api/[controller]")]
public class DataController(ILogger<DataController> logger, IDataControl dataControl) : ControllerBase
{
    // POST api/data/subscribe
    [HttpPost("subscribe")]
    public async Task<object?> Subscribe([FromBody] DataDescription dataDescription, CancellationToken token)
    {
        logger.LogWarning($"{DateTime.Now} GetInput for {dataDescription}");
        return await dataControl.SubscribeAsync(dataDescription, token);
    }
}
