using Microsoft.AspNetCore.Server.Kestrel.Core;
using System.Runtime.CompilerServices;
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
    private readonly ILogger<LedControl> _logger;

    private readonly AdsClient _adsClient;
    private readonly Dictionary<LedId, LedState> _ledStates;
    private readonly List<LedId> _brightMode;
    private readonly List<LedId> _outputsSwapped;

    public LedControl(ILogger<LedControl> logger)
    {
        _logger = logger;

        _adsClient = new AdsClient();
        _adsClient.Connect(AmsNetId.Local, 851);

        _ledStates = new Dictionary<LedId, LedState>();
        foreach(var ledId in Enum.GetValues<LedId>()) {
            _ledStates.Add(ledId, new LedState(0.5, 0.5));
        }

        _brightMode = new List<LedId>()
        {   
            LedId.Eg12,
            LedId.Og16
        };

        _outputsSwapped = new List<LedId>()
        {
        };
    }

    public async Task OnAsync(LedId id, CancellationToken token)
    {
        await Set(id, token);
    }

    public async Task OffAsync(LedId id, CancellationToken token)
    {
        await SetValueOnPlcAsync(GetFloor(id), (GetNumber(id) * 2) - 1, 0, token);
        await SetValueOnPlcAsync(GetFloor(id), (GetNumber(id) * 2), 0, token);
    }

    public async Task LevelAsync(LedId id, double brightness, CancellationToken token)
    {
        _ledStates[id].Brightness = brightness;
        await Set(id, token);
    }

    public async Task TemperatureAsync(LedId id, double temperature, CancellationToken token)
    {
        _ledStates[id].Temperature = temperature;
        await Set(id, token);
    }

    private async Task Set(LedId id, CancellationToken token)
    {
        double temperature = _ledStates[id].Temperature;
        double brightness = _ledStates[id].Brightness;

        double coldWhite;
        double warmWhite;

        if (_brightMode.Contains(id))
        {
            // Temperatur 0 -> Kaltweiss, 1 -> Warmweiss
            // Maximale Helligkeit bei einer Temperatur von  0.22 - 0.27
            coldWhite = double.Min(1.0, (1.0 / (0.27 - 1.0)) * (temperature - 0.27) + 1.0);
            warmWhite = double.Min(1.0, (1.0 / 0.22) * temperature);
        }
        else
        {
            coldWhite = 1 - temperature;
            warmWhite = temperature;
        }

        int channel1;
        int channel2;
        if(_outputsSwapped.Contains(id))
        {
            channel1 = (int)(coldWhite * brightness * short.MaxValue);
            channel2 = (int)(warmWhite * brightness * short.MaxValue);
        }
        else
        {
            channel1 = (int)(warmWhite * brightness * short.MaxValue);
            channel2 = (int)(coldWhite * brightness * short.MaxValue);
        }

        await SetValueOnPlcAsync(GetFloor(id), (GetNumber(id) * 2) - 1, channel1, token);
        await SetValueOnPlcAsync(GetFloor(id), (GetNumber(id) * 2), channel2, token);

        var s = $"Temp: {temperature:F3}, bright: {brightness:F3} cold: {coldWhite:F3} warm: {warmWhite:F3}, ch1: {channel1:D5}, ch2: {channel2:D5}";
        _logger.LogWarning(s);
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
