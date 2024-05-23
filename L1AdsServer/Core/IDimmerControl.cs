namespace L1AdsServer.Core;

public enum DimmerId
{
    HvUg1,
    HvUg2,
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
    UvEg15,
    UvEg16,
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
    UvOg18
}

public interface IDimmerControl
{
    Task OnAsync(DimmerId id, CancellationToken token);
    Task OffAsync(DimmerId id, CancellationToken token);
    Task SetAsync(DimmerId id, ushort value, CancellationToken token);

    Task<uint> GetAsync(int dimmer, CancellationToken token);
}
