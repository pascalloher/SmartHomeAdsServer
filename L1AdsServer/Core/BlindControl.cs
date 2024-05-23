using L1AdsServer.Core.NewFolder;
using TwinCAT.Ads;

namespace L1AdsServer.Core;

public class BlindControl : IBlindControl
{
    private readonly IDataExtractor _dataExtractor;
    private readonly AdsClient _adsClient;


    public BlindControl(IDataExtractor dataExtractor)
    {
        _dataExtractor = dataExtractor;

        _adsClient = new AdsClient();
        _adsClient.Connect(AmsNetId.Local, 851);
    }

    public async Task OpenAsync(BlindId id, CancellationToken token)
    {
        await SetCloseOnPlcAsync(id, false, token);
        await Task.Delay(2000);
        await SetOpenOnPlcAsync(id, true, token);
    }

    public async Task CloseAsync(BlindId id, CancellationToken token)
    {
        await SetOpenOnPlcAsync(id, false, token);
        await Task.Delay(2000);
        await SetCloseOnPlcAsync(id, true, token);
    }

    public async Task StopAsync(BlindId id, CancellationToken token)
    {
        await SetOpenOnPlcAsync(id, false, token);
        await SetCloseOnPlcAsync(id, false, token);
    }

    private async Task SetOpenOnPlcAsync(BlindId id, bool value, CancellationToken token)
    {
        string variableName = _dataExtractor.CreateVariableName(id.ToString(), "BlindOpen", out bool _, out VariableInfo _);
        var result = await _adsClient.WriteValueAsync(variableName, value, token);
        result.ThrowOnError();
    }

    private async Task SetCloseOnPlcAsync(BlindId id, bool value, CancellationToken token)
    {
        string variableName = _dataExtractor.CreateVariableName(id.ToString(), "BlindClose", out bool _, out VariableInfo _);
        var result = await _adsClient.WriteValueAsync(variableName, value, token);
        result.ThrowOnError();
    }
}
