namespace L1AdsServer.Core;

public enum BlindId
{
    Ug1,
    Ug2,
    Ga1,
    Ga2,
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
    Og13
}

public interface IBlindControl
{
    Task OpenAsync(BlindId id, CancellationToken token);
    Task CloseAsync(BlindId id, CancellationToken token);
    Task StopAsync(BlindId id, CancellationToken token);
}
