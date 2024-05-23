using L1AdsServer.Core.NewFolder;
using TwinCAT.Ads;

namespace L1AdsServer.Core;

public class DimmerControl : IDimmerControl
{
    private readonly IDataExtractor _dataExtractor;
    private readonly Dictionary<DimmerId, ushort> _dimmerValues;
    private readonly AdsClient _adsClient;

    public DimmerControl(IDataExtractor dataExtractor)
    {
        _dataExtractor = dataExtractor;
        _dimmerValues = new Dictionary<DimmerId, ushort>(Enum.GetValues<DimmerId>().Length);
        foreach (var dimmerId in Enum.GetValues<DimmerId>())
        {
            _dimmerValues.Add(dimmerId, 30000);
        }

        _adsClient = new AdsClient();
        _adsClient.Connect(AmsNetId.Local, 851);
    }

    public async Task<uint> GetAsync(int dimmer, CancellationToken token)
    {
        ResultValue<uint> readResult = await _adsClient.ReadValueAsync<uint>($"Main.Dimmer[{dimmer}]", token);
        readResult.ThrowOnError();
        return readResult.Value;
    }

    public async Task OnAsync(DimmerId id, CancellationToken token)
    {
        await SetOnPlcAsync(id, _dimmerValues.GetValueOrDefault(id), token);
    }

    public async Task OffAsync(DimmerId id, CancellationToken token)
    {
        await SetOnPlcAsync(id, 0, token);
    }

    public async Task SetAsync(DimmerId id, ushort value, CancellationToken token)
    {
        _dimmerValues[id] = value;
        await SetOnPlcAsync(id, value, token);
    }

    private async Task SetOnPlcAsync(DimmerId id, ushort value, CancellationToken token)
    {
        string variableName = _dataExtractor.CreateVariableName(id.ToString(), "Dimmer", out bool _, out VariableInfo _);
        var result = await _adsClient.WriteValueAsync(variableName, value, token);
        result.ThrowOnError();
    }
}
