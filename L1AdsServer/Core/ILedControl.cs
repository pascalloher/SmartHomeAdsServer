namespace L1AdsServer.Core;

public enum LedId
{
    Og1,
    Og2,
    Og3,
    Og4,
    Og5
}

public interface ILedControl
{
    Task OnAsync(LedId id, CancellationToken token);
    Task OffAsync(LedId id, CancellationToken token);
    Task LevelAsync(LedId id, double brightness, CancellationToken token);
    Task TemperatureAsync(LedId id, double colorTemp, CancellationToken token);
}
