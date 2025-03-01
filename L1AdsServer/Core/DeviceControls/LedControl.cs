using L1AdsServer.Core.Common;
using TwinCAT.Ads;

namespace L1AdsServer.Core.Controls;

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

    private readonly IDataExtractor _dataExtractor;
    private readonly Dictionary<LedId, LedState> _ledStates;
    private readonly List<LedId> _brightMode = [LedId.UvEg12, LedId.UvOg16];

    public LedControl(ILogger<LedControl> logger, IDataExtractor dataExtractor)
    {
        _logger = logger;
        _dataExtractor = dataExtractor;

        _ledStates = [];
        foreach(var ledId in Enum.GetValues<LedId>()) {
            _ledStates.Add(ledId, new LedState(0.5, 0.5));
        }
    }

    public async Task OnAsync(LedId id, CancellationToken token)
    {
        await Set(id, token);
    }

    public async Task OffAsync(LedId id, CancellationToken token)
    {
        await SetValueOnPlcAsync(id, 0, 0, token);
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
            coldWhite = double.Min(1.0, 1.0 / (0.27 - 1.0) * (temperature - 0.27) + 1.0);
            warmWhite = double.Min(1.0, 1.0 / 0.22 * temperature);
        }
        else
        {
            coldWhite = 1 - temperature;
            warmWhite = temperature;
        }

        int warmWhiteInt = (int)(warmWhite * brightness * short.MaxValue);
        int coldWhiteInt = (int)(coldWhite * brightness * short.MaxValue);

        await SetValueOnPlcAsync(id, warmWhiteInt, coldWhiteInt, token);

        var s = $"Temp: {temperature:F3}, bright: {brightness:F3} cold: {coldWhite:F3} warm: {warmWhite:F3}, ch1: {warmWhiteInt:D5}, ch2: {coldWhiteInt:D5}";
        _logger.LogInformation(new EventId(1626838989), s);
    }

    private async Task SetValueOnPlcAsync(LedId id, int warmWhite, int coldWhite, CancellationToken token)
    {
        using var adsClient = new AdsClient();
        adsClient.Connect(AmsNetId.Local, 851);

        var variableNameWarmWhite = _dataExtractor.CreateVariableName(id.ToString(), "LedWw", out bool _, out VariableInfo _);
        var variableNameColdWhite = _dataExtractor.CreateVariableName(id.ToString(), "LedCw", out bool _, out VariableInfo _);

        var result = await adsClient.WriteValueAsync(variableNameWarmWhite, warmWhite, token);
        result.ThrowOnError();
        result = await adsClient.WriteValueAsync(variableNameColdWhite, coldWhite, token);
        result.ThrowOnError();
    }
}
