namespace L1AdsServer.Core;

public enum SwitchId
{
    UvEg1 = 0, // 24V LED OG
    UvEg2 = 1, // 24V LED EG
    UvEg3 = 2, // Hauseingangstür Tag/Nacht
    HvUg1 = 3, // 24V Supply LED's
    HvUg2 = 4, // Light Technik
    HvUg3 = 5, // Light Keller
    HvUg4 = 6, // Light Werken
    HvUg28 = 28, // Geschaltete Phase Technik Nord (Luftentfeuchter)
}

public interface ISwitchControl
{
    Task OnAsync(SwitchId id, CancellationToken token);
    Task OffAsync(SwitchId id, CancellationToken token);
}
