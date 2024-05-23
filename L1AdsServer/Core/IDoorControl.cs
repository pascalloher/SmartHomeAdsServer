namespace L1AdsServer.Core;

public enum DoorId
{
    UvGa1 = 0,
    UvGa2 = 1,
    UvEg1 = 2,
}

public enum DoorState
{
    Undefined = 0,
    Closed = 1,
    Opening = 2,
    Opened = 3,
    Closing = 4,
    Intermediate = 5
}

public interface IDoorControl
{
    Task OpenAsync(DoorId id, CancellationToken token);
    Task CloseAsync(DoorId id, CancellationToken token);
    Task StopAsync(DoorId id, CancellationToken token);
}
