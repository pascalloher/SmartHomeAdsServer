using L1AdsServer.Core.Common;
using L1AdsServer.Core.Plc;
using TwinCAT.Ads;

namespace L1AdsServer.Core.Controls;

public class DimmerControl : IDimmerControl
{
    private readonly IDataExtractor _dataExtractor;
    private readonly IAdsService _adsService;

    private readonly Dictionary<DimmerId, ushort> _dimmerValues;

    public DimmerControl(IDataExtractor dataExtractor, IAdsService adsService)
    {
        _dataExtractor = dataExtractor;
        _adsService = adsService;

        _dimmerValues = new Dictionary<DimmerId, ushort>(Enum.GetValues<DimmerId>().Length);
        foreach (var dimmerId in Enum.GetValues<DimmerId>())
        {
            _dimmerValues.Add(dimmerId, 30000);
        }
    }

    public async Task<uint> GetAsync(int dimmer, CancellationToken token)
    {
        return await _adsService.ReadValueAsync<uint>($"Main.Dimmer[{dimmer}]", token);
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
        var result = await _adsService.WriteValueAsync(variableName, value, token);
        result.ThrowOnError();
    }
}
