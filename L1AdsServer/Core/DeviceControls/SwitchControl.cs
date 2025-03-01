using L1AdsServer.Core.Common;
using TwinCAT.Ads;

namespace L1AdsServer.Core.Controls;

public class SwitchControl : ISwitchControl
{
    private readonly ILogger<SwitchControl> _logger;
    private readonly IDataExtractor _dataExtractor;

    public SwitchControl(ILogger<SwitchControl> logger, IDataExtractor dataExtractor)
    {
        _logger = logger;
        _dataExtractor = dataExtractor;
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
        using var adsClient = new AdsClient();
        adsClient.Connect(AmsNetId.Local, 851);

        string variableName = _dataExtractor.CreateVariableName(id.ToString(), "Out", out bool _, out VariableInfo _);
        var result = await adsClient.WriteValueAsync(variableName, value, token);
        result.ThrowOnError();
    }
}
