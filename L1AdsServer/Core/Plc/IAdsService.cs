using System.Diagnostics.CodeAnalysis;
using TwinCAT.Ads;

namespace L1AdsServer.Core.Plc;

public interface IAdsService : IDisposable
{
    event EventHandler<AdsNotificationEventArgs> AdsNotification;
    IObservable<ulong?> PlcRestartDetected { get; }

    Task<ResultWrite> WriteValueAsync<T>(string symbolPath, [DisallowNull] T value, CancellationToken Token) where T : notnull;
    Task<T?> ReadValueAsync<T>(string symbolPath, CancellationToken token = default);
    public Task<ResultHandle> AddDeviceNotificationAsync(string symbolPath, int dataSize, NotificationSettings settings, object? userData, CancellationToken token);
}