namespace L1AdsServer.Core;

public enum DimmerId
{
    UG1,
    EG1,
    EG2,
    EG3,
    EG4,
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
    OG13,
    OG14,
    OG15,
    OG16,
    OG17,
    OG18
}

public enum Floor
{
    UG,
    GA,
    EG,
    OG
}

public interface IDimmerControl
{
    Task OnAsync(DimmerId id, CancellationToken token);
    Task OffAsync(DimmerId id, CancellationToken token);
    Task SetAsync(DimmerId id, ushort value, CancellationToken token);

    Task<uint> GetAsync(int dimmer, CancellationToken token);
}
