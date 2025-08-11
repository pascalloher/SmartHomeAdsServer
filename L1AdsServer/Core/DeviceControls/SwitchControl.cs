using L1AdsServer.Core.Common;
using L1AdsServer.Core.Plc;
using TwinCAT.Ads;

namespace L1AdsServer.Core.Controls;

public class SwitchControl : ISwitchControl
{
    private readonly ILogger<SwitchControl> _logger;
    private readonly IDataExtractor _dataExtractor;
    private readonly IAdsService _adsService;

    public SwitchControl(ILogger<SwitchControl> logger, IDataExtractor dataExtractor, IAdsService adsService)
    {
        _logger = logger;
        _dataExtractor = dataExtractor;
        _adsService = adsService;
    }

    public async Task OnAsync(SwitchId id, CancellationToken token)
    {
        await SetValueOnPlcAsync(id, 1, token);
    }

    public async Task OffAsync(SwitchId id, CancellationToken token)
    {
        await SetValueOnPlcAsync(id, 0, token);
    }

    private async Task SetValueOnPlcAsync(SwitchId id, int value, CancellationToken token)
    {
        string variableName = _dataExtractor.CreateVariableName(id.ToString(), "Out", out bool _, out VariableInfo _);
        await _adsService.WriteValueAsync(variableName, value, token);
    }
}
