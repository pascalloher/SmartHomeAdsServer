namespace L1AdsServer.Core;

public enum SwitchId
{
    Uv1 = 0,
    Uv2 = 1,
    Eg1 = 2, // Main Door Open
}

public interface ISwitchControl
{
    Task OnAsync(SwitchId id, CancellationToken token);
    Task OffAsync(SwitchId id, CancellationToken token);
}
