namespace L1AdsServer.Core;

public enum BlindId
{
    UG1,
    UG2,
    EG1,
    EG2,
    EG3,
    EG4,
    EG5,
    EG6,
    EG7,
    EG8,
    EG9,
    EG10,
    EG11,
    EG12,
    EG13,
    EG14,
    EG15,
    EG16,
    OG1,
    OG2,
    OG3,
    OG4,
    OG5,
    OG6,
    OG7,
    OG8,
    OG9,
    OG10,
    OG11,
    OG12,
    OG13
}

public interface IBlindControl
{
    Task OpenAsync(BlindId id, CancellationToken token);
    Task CloseAsync(BlindId id, CancellationToken token);
    Task StopAsync(BlindId id, CancellationToken token);
}
