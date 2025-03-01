namespace L1AdsServer.Core.Controls;

public enum BlindId
{
    HvUg1,
    HvUg2,
    UvGa1,
    UvGa2,
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
    UvOg13
}

public interface IBlindControl
{
    Task OpenAsync(BlindId id, CancellationToken token);
    Task CloseAsync(BlindId id, CancellationToken token);
    Task StopAsync(BlindId id, CancellationToken token);
}
