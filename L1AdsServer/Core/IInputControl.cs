namespace L1AdsServer.Core;

public class DeviceInfo
{
    public string DeviceName { get; set; } = String.Empty;
}

public enum InputId
{
    UvEg1 = 0,  // 
    UvEg31 = 30 // eg_office_window_door
}

public interface IInputControl
{
    Task<bool> GetAsync(InputId id, DeviceInfo deviceInfo, CancellationToken token);
}
