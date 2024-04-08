
namespace L1AdsServer.Core;

public enum InputId
{
    UV_EG1 = 0, // Main Door Locked
}

public interface IInputControl
{
    Task<bool> GetAsync(InputId id, CancellationToken token);
}
