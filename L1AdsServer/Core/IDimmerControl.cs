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
    OG3
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
