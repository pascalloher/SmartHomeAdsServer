namespace L1AdsServer.Core;

public enum SwitchId
{
    Uv1 = 0,
    Uv2 = 1,
    Eg1 = 2, // Main Door Open
    Ug1 = 3, // 24V Supply LED's
    Ug2 = 4, // Light Technik
    Ug3 = 5, // Light Keller
    Ug4 = 6, // Light Werken
}

public interface ISwitchControl
{
    Task OnAsync(SwitchId id, CancellationToken token);
    Task OffAsync(SwitchId id, CancellationToken token);
}
