using TwinCAT.Ads;

namespace L1AdsServer.Core;

public class LedState {
    public double Brightness { get; set; }
    public double Temperature { get; set; }

    public LedState(double brightness, double temperature)
    {
        Brightness = brightness;
        Temperature = temperature;
    }
}

public class LedControl : ILedControl
{
    private readonly AdsClient _adsClient;
    private readonly Dictionary<LedId, LedState> _ledStates;

    public LedControl()
    {
        _adsClient = new AdsClient();
        _adsClient.Connect(AmsNetId.Local, 851);

        _ledStates = new Dictionary<LedId, LedState>();
        foreach(var ledId in Enum.GetValues<LedId>()) {
            _ledStates.Add(ledId, new LedState(0.5, 0.5));
        }
    }

    public async Task OnAsync(LedId id, CancellationToken token)
    {
        await SetValueOnPlcAsync(GetFloor(id), (GetNumber(id) * 2) - 1, (int)(_ledStates[id].Brightness * 32640 * _ledStates[id].Temperature), token);
        await SetValueOnPlcAsync(GetFloor(id), (GetNumber(id) * 2), (int)(_ledStates[id].Brightness * 32640 * (1 - _ledStates[id].Temperature)), token);
    }

    public async Task OffAsync(LedId id, CancellationToken token)
    {
        await SetValueOnPlcAsync(GetFloor(id), (GetNumber(id) * 2) - 1, 0, token);
        await SetValueOnPlcAsync(GetFloor(id), (GetNumber(id) * 2), 0, token);
    }

    public async Task LevelAsync(LedId id, double brightness, CancellationToken token)
    {
        _ledStates[id].Brightness = brightness;
        await SetValueOnPlcAsync(GetFloor(id), (GetNumber(id) * 2) - 1, (int)(_ledStates[id].Brightness * 32640 * _ledStates[id].Temperature), token);
        await SetValueOnPlcAsync(GetFloor(id), (GetNumber(id) * 2), (int)(_ledStates[id].Brightness * 32640 * (1 - _ledStates[id].Temperature)), token);
    }

    public async Task TemperatureAsync(LedId id, double temperature, CancellationToken token)
    {
        _ledStates[id].Temperature = temperature;
        await SetValueOnPlcAsync(GetFloor(id), (GetNumber(id) * 2) - 1, (int)(_ledStates[id].Brightness * 32640 * _ledStates[id].Temperature), token);
        await SetValueOnPlcAsync(GetFloor(id), (GetNumber(id) * 2), (int)(_ledStates[id].Brightness * 32640 * (1 - _ledStates[id].Temperature)), token);
    }

    private async Task SetValueOnPlcAsync(Floor floor, int channel, int value, CancellationToken token)
    {
        var variableName = $"GVL_UV.{floor}Led[{channel}]";
        var result = await _adsClient.WriteValueAsync(variableName, value, token);
        result.ThrowOnError();
    }

    private static Floor GetFloor(LedId id)
    {
        string floorString = id.ToString()[..2];

        if (Enum.TryParse(floorString, out Floor floor))
        {
            return floor;
        }
        else
        {
            throw new ArgumentException($"Invalid BlindId '{id}'");
        }
    }

    private static int GetNumber(LedId id)
    {
        // Assuming the numeric part starts from the third character
        if (int.TryParse(id.ToString()[2..], out int number))
        {
            return number;
        }
        else
        {
            // Handle the case where the numeric part is not a valid integer
            throw new ArgumentException($"Invalid LedId '{id}'");
        }
    }
}
