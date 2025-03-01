using L1AdsServer.Core.Common;
using TwinCAT.Ads;

namespace L1AdsServer.Core.Controls;

public class DoorControl : IDoorControl
{
    private readonly IDataExtractor _dataExtractor;
    private readonly Dictionary<DoorId, DoorState> _doorStates;

    public DoorControl(IDataExtractor dataExtractor)
    {
        _dataExtractor = dataExtractor;
        _doorStates = [];
        foreach(var doorId in Enum.GetValues<DoorId>())
        {
            _doorStates.Add(doorId, DoorState.Undefined);
        }
    }

    public async Task OpenAsync(DoorId id, CancellationToken token)
    {
        if (id == DoorId.UvEg1)
        {
            await SetOpenOnPlcAsync(id, true, token);
            await Task.Delay(200, token);
            await SetOpenOnPlcAsync(id, false, token);
        }
        else
        {
            // Eingang an der Hörmann 560 Garagentorsteuerung ist low aktiv
            await SetOpenOnPlcAsync(id, false, token);
            await Task.Delay(200, token);
            await SetOpenOnPlcAsync(id, true, token);
            _doorStates[id] = DoorState.Opening;
        }
    }

    public async Task CloseAsync(DoorId id, CancellationToken token)
    {
        // Eingang an der Hörmann 560 Garagentorsteuerung ist low aktiv
        await SetCloseOnPlcAsync(id, false, token);
        await Task.Delay(200, token);
        await SetCloseOnPlcAsync(id, true, token);
        _doorStates[id] = DoorState.Closing;
    }

    public async Task StopAsync(DoorId id, CancellationToken token)
    {
        // Stop-Eingang an der Hörmann 560 Garagentorsteuerung ist low aktiv und normally-closed
        await SetStopOnPlcAsync(id, false, token);
        await Task.Delay(200, token);
        await SetStopOnPlcAsync(id, true, token);
    }

    private async Task SetOpenOnPlcAsync(DoorId id, bool value, CancellationToken token)
    {
        using var adsClient = new AdsClient();
        adsClient.Connect(AmsNetId.Local, 851);

        var variableName = _dataExtractor.CreateVariableName(id.ToString(), "DoorOpen", out bool _, out VariableInfo _);
        var result = await adsClient.WriteValueAsync(variableName, value, token);
        result.ThrowOnError();
    }

    private async Task SetCloseOnPlcAsync(DoorId id, bool value, CancellationToken token)
    {
        using var adsClient = new AdsClient();
        adsClient.Connect(AmsNetId.Local, 851);

        var variableName = _dataExtractor.CreateVariableName(id.ToString(), "DoorClose", out bool _, out VariableInfo _);
        var result = await adsClient.WriteValueAsync(variableName, value, token);
        result.ThrowOnError();
    }
    private async Task SetStopOnPlcAsync(DoorId id, bool value, CancellationToken token)
    {
        using var adsClient = new AdsClient();
        adsClient.Connect(AmsNetId.Local, 851);

        var variableName = _dataExtractor.CreateVariableName(id.ToString(), "DoorStop", out bool _, out VariableInfo _);
        var result = await adsClient.WriteValueAsync(variableName, value, token);
        result.ThrowOnError();
    }
}
