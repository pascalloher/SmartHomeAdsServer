
namespace L1AdsServer.Core;

public enum InputId
{
    UvEg1 = 0, // Main Door Locked
    UvEg31 = 30 // eg_office_window_door
}

public interface IInputControl
{
    Task<bool> GetAsync(InputId id, CancellationToken token);
}
