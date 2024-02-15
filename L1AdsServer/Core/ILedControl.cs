namespace L1AdsServer.Core;

public enum LedId
{
    Eg1,
    Eg2,
    Eg3,
    Eg4,
    Eg5,
    Eg6,
    Eg7,
    Eg8,
    Eg9,
    Eg10,
    Og1,
    Og2,
    Og3,
    Og4,
    Og5,
    Og6,
    Og7,
    Og8,
    Og9,
    Og10,
    Og11,
    Og12,
    Og13,
    Og14
}

public interface ILedControl
{
    Task OnAsync(LedId id, CancellationToken token);
    Task OffAsync(LedId id, CancellationToken token);
    Task LevelAsync(LedId id, double brightness, CancellationToken token);
    Task TemperatureAsync(LedId id, double colorTemp, CancellationToken token);
}
