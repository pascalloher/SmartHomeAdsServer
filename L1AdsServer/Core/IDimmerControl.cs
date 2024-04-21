namespace L1AdsServer.Core;

public enum DimmerId
{
    Ug1,
    Ug2,
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
    Eg11,
    Eg12,
    Eg13,
    Eg14,
    Eg15,
    Eg16,
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
    Og14,
    Og15,
    Og16,
    Og17,
    Og18
}

public enum Floor
{
    Ug,
    Ga,
    Eg,
    Og
}

public interface IDimmerControl
{
    Task OnAsync(DimmerId id, CancellationToken token);
    Task OffAsync(DimmerId id, CancellationToken token);
    Task SetAsync(DimmerId id, ushort value, CancellationToken token);

    Task<uint> GetAsync(int dimmer, CancellationToken token);
}
