namespace L1AdsServer.Core;

public enum LedId
{
    UvEg1,
    UvEg2,
    UvEg3,
    UvEg4,
    UvEg5,
    UvEg6,
    UvEg7,
    UvEg8,
    UvEg9,
    UvEg10,
    UvEg11,
    UvEg12,
    UvEg13,
    UvEg14,
    UvOg1,
    UvOg2,
    UvOg3,
    UvOg4,
    UvOg5,
    UvOg6,
    UvOg7,
    UvOg8,
    UvOg9,
    UvOg10,
    UvOg11,
    UvOg12,
    UvOg13,
    UvOg14,
    UvOg15,
    UvOg16,
    UvOg17,
    UvOg18,
}

public interface ILedControl
{
    Task OnAsync(LedId id, bool brightMode, CancellationToken token);
    Task OffAsync(LedId id, CancellationToken token);
    Task LevelAsync(LedId id, double brightness, bool brightMode, CancellationToken token);
    Task TemperatureAsync(LedId id, double colorTemp, bool brightMode, CancellationToken token);
}
