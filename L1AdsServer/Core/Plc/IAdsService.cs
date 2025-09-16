using System.Diagnostics.CodeAnalysis;
using TwinCAT.Ads;

namespace L1AdsServer.Core.Plc;

public interface IAdsService : IDisposable
{
    event EventHandler<AdsNotificationEventArgs> AdsNotification;

    Task<ResultWrite> WriteValueAsync<T>(string symbolPath, [DisallowNull] T value, CancellationToken token = default) where T : notnull;
    Task<T> ReadValueAsync<T>(string symbolPath, CancellationToken token = default) where T : notnull;
    public Task<ResultHandle> AddDeviceNotificationAsync(string symbolPath, int dataSize, NotificationSettings settings, object? userData, CancellationToken token);
    void ReConnect();
}