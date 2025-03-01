using L1AdsServer.Controllers;

namespace L1AdsServer.Core.Controls;

public interface IDataControl
{
    Task<object?> SubscribeAsync(DataDescription dataDescription, CancellationToken token);
}