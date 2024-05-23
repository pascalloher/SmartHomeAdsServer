using L1AdsServer.Core.NewFolder;
using TwinCAT.Ads;

namespace L1AdsServer.Core;

public class SwitchControl : ISwitchControl
{
    private readonly ILogger<SwitchControl> _logger;
    private readonly IDataExtractor _dataExtractor;
    private readonly AdsClient _adsClient;

    public SwitchControl(ILogger<SwitchControl> logger, IDataExtractor dataExtractor)
    {
        _logger = logger;
        _dataExtractor = dataExtractor;

        _adsClient = new AdsClient();
        _adsClient.Connect(AmsNetId.Local, 851);
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
        var result = await _adsClient.WriteValueAsync(variableName, value, token);
        result.ThrowOnError();
    }
}
