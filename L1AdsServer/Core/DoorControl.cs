using TwinCAT.Ads;

namespace L1AdsServer.Core;

public class DoorControl : IDoorControl
{
    private readonly AdsClient _adsClient;
    private readonly Dictionary<DoorId, DoorState> _doorStates;

    public DoorControl()
    {
        _adsClient = new AdsClient();
        _adsClient.Connect(AmsNetId.Local, 851);

        _doorStates = new Dictionary<DoorId, DoorState>();
        foreach(var doorId in Enum.GetValues<DoorId>())
        {
            _doorStates.Add(doorId, DoorState.Undefined);
        }
    }

    public async Task OpenAsync(DoorId id, CancellationToken token)
    {
        if (id == DoorId.Eg1)
        {
            await SetOpenOnPlcAsync(id, true, token);
            await Task.Delay(200);
            await SetOpenOnPlcAsync(id, false, token);
        }
        else
        {
            // Eingang an der Hörmann 560 Garagentorsteuerung ist low aktiv
            await SetOpenOnPlcAsync(id, false, token);
            await Task.Delay(200);
            await SetOpenOnPlcAsync(id, true, token);
            _doorStates[id] = DoorState.Opening;
        }
    }

    public async Task CloseAsync(DoorId id, CancellationToken token)
    {
        // Eingang an der Hörmann 560 Garagentorsteuerung ist low aktiv
        await SetCloseOnPlcAsync(id, false, token);
        await Task.Delay(200);
        await SetCloseOnPlcAsync(id, true, token);
        _doorStates[id] = DoorState.Closing;
    }

    public async Task StopAsync(DoorId id, CancellationToken token)
    {
        if (_doorStates[id] == DoorState.Closing)
        {
            await CloseAsync(id, token);
        }
        else if (_doorStates[id] == DoorState.Opening)
        { 
            await OpenAsync(id, token);
        }
    }

    private async Task SetOpenOnPlcAsync(DoorId id, bool value, CancellationToken token)
    {
        var variableName = $"GVL_UV.{GetFloor(id)}DoorOpen[{GetNumber(id)}]";
        var result = await _adsClient.WriteValueAsync(variableName, value, token);
        result.ThrowOnError();
    }

    private async Task SetCloseOnPlcAsync(DoorId id, bool value, CancellationToken token)
    {
        var variableName = $"GVL_UV.{GetFloor(id)}DoorClose[{GetNumber(id)}]";
        var result = await _adsClient.WriteValueAsync(variableName, value, token);
        result.ThrowOnError();
    }

    private static Floor GetFloor(DoorId id)
    {
        string floorString = id.ToString()[..2];

        if (Enum.TryParse(floorString, out Floor floor))
        {
            return floor;
        }
        else
        {
            throw new ArgumentException($"Invalid DoorId '{id}'");
        }
    }

    private static int GetNumber(DoorId id)
    {
        // Assuming the numeric part starts from the third character
        if (int.TryParse(id.ToString()[2..], out int number))
        {
            return number;
        }
        else
        {
            // Handle the case where the numeric part is not a valid integer
            throw new ArgumentException($"Invalid DoorId '{id}'");
        }
    }
}
