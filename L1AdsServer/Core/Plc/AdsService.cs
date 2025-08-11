using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using System.Reactive.Subjects;
using TwinCAT.Ads;
using L1AdsServer.Controllers;
using System.Reactive.Linq;

namespace L1AdsServer.Core.Plc;

public class ReadRequest<T>
{
    public string SymbolPath { get; }
    public TaskCompletionSource<T> Tcs { get; }
    public CancellationToken Token { get; }

    public ReadRequest(string symbolPath, TaskCompletionSource<T> tcs, CancellationToken token)
    {
        SymbolPath = symbolPath;
        Tcs = tcs;
        Token = token;
    }
}

// Base interface für type-erased read requests
public interface IReadRequest
{
    string SymbolPath { get; }
    CancellationToken Token { get; }
    Task ProcessAsync(AdsClient adsClient);
}

public class TypedReadRequest<T> : IReadRequest
{
    public string SymbolPath { get; }
    public TaskCompletionSource<T?> Tcs { get; }
    public CancellationToken Token { get; }

    public TypedReadRequest(string symbolPath, TaskCompletionSource<T?> tcs, CancellationToken token)
    {
        SymbolPath = symbolPath;
        Tcs = tcs;
        Token = token;
    }

    public async Task ProcessAsync(AdsClient adsClient)
    {
        try
        {
            var result = await adsClient.ReadValueAsync<T>(SymbolPath, Token);
            Tcs.SetResult(result.Value);
        }
        catch (OperationCanceledException)
        {
            Tcs.SetCanceled(Token);
        }
        catch (Exception ex)
        {
            Tcs.SetException(ex);
        }
    }
}

public class AdsService : IAdsService, IDisposable
{
    private readonly ILogger<AdsService> _logger;
    private readonly AdsClient _adsClient;

    // Unified request processing - ALLE ADS-Operationen über eine Queue
    private readonly Channel<Func<AdsClient, Task>> _adsOperationChannel;
    private readonly Task _adsProcessingTask;

    private readonly CancellationTokenSource _processingCts;

    // Heartbeat-Monitor
    private readonly Timer _heartbeatTimer;
    private readonly Subject<ulong?> _plcRestartDetected;
    private ulong? _lastHeartbeat;
    private bool _firstHeartbeatRun = true;

    private bool _disposed;

    // Event als nullbar deklarieren, um den Fehler zu beheben
    public event EventHandler<AdsNotificationEventArgs>? AdsNotification;

    IObservable<ulong?> IAdsService.PlcRestartDetected => _plcRestartDetected.AsObservable();

    public AdsService(ILogger<AdsService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _adsClient = new AdsClient();

        // Event des AdsClient abonnieren
        _adsClient.AdsNotification += OnAdsNotification;

        try
        {
            _adsClient.Connect(AmsNetId.Local, 851);
        }
        catch (Exception ex)
        {
            _logger.LogError(new EventId(968904576), ex, "Failed to connect to ADS client.");
            _adsClient.AdsNotification -= OnAdsNotification; // Event-Handler bei Fehler entfernen
            _adsClient.Dispose();
            throw new InvalidOperationException("Failed to initialize ADS connection.", ex);
        }

        _processingCts = new CancellationTokenSource();

        // Unified ADS Operation Queue - sequenzieller Zugriff auf AdsClient
        _adsOperationChannel = Channel.CreateBounded<Func<AdsClient, Task>>(new BoundedChannelOptions(200)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest,
        });

        _adsProcessingTask = Task.Run(() => ProcessAdsOperationsAsync(_processingCts.Token));

        // Heartbeat-Monitor
        _plcRestartDetected = new Subject<ulong?>();
        _heartbeatTimer = new Timer(CheckHeartbeat, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

        _logger.LogInformation("AdsService initialized with thread-safe sequential ADS access");
    }

    protected virtual void OnAdsNotification(object? sender, AdsNotificationEventArgs e)
    {
        AdsNotification?.Invoke(this, e);
    }

    public async Task<ResultWrite> WriteValueAsync<T>(string symbolPath, [DisallowNull] T value, CancellationToken token = default)
        where T : notnull
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(AdsService));

        if (string.IsNullOrWhiteSpace(symbolPath))
            throw new ArgumentException("Symbol path cannot be null or whitespace.", nameof(symbolPath));

        if (value is null)
        {
            _logger.LogError(new EventId(968904573), "Attempted to write a null value for symbol '{SymbolPath}'.", symbolPath);
            throw new ArgumentNullException(nameof(value), "Value cannot be null.");
        }

        try
        {
            var tcs = new TaskCompletionSource<ResultWrite>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _processingCts.Token);

            // Write-Operation als Lambda für die Queue
            Func<AdsClient, Task> writeOperation = async (adsClient) =>
            {
                try
                {
                    var result = await adsClient.WriteValueAsync(symbolPath, value, linkedCts.Token);
                    tcs.SetResult(result);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning(new EventId(968904575), "Write operation for symbol '{SymbolPath}' was canceled.", symbolPath);
                    tcs.SetCanceled(linkedCts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(new EventId(968904577), ex, "Failed to write value for symbol '{SymbolPath}'.", symbolPath);
                    tcs.SetException(ex);
                }
            };

            await _adsOperationChannel.Writer.WriteAsync(writeOperation, token);
            return await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(new EventId(968904574), "Write operation for symbol '{SymbolPath}' was canceled.", symbolPath);
            throw new TaskCanceledException("The write operation was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(new EventId(1322640845), ex, "Failed to queue write operation for symbol '{SymbolPath}'.", symbolPath);
            throw new InvalidOperationException($"Failed to queue write operation for symbol '{symbolPath}'.", ex);
        }
    }

    public async Task<T?> ReadValueAsync<T>(string symbolPath, CancellationToken token = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(AdsService));

        if (string.IsNullOrWhiteSpace(symbolPath))
            throw new ArgumentException("Symbol path cannot be null or whitespace.", nameof(symbolPath));

        try
        {
            var tcs = new TaskCompletionSource<T?>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _processingCts.Token);

            // Read-Operation als Lambda für die Queue
            Func<AdsClient, Task> readOperation = async (adsClient) =>
            {
                try
                {
                    var result = await adsClient.ReadValueAsync<T>(symbolPath, linkedCts.Token);
                    tcs.SetResult(result.Value);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning(new EventId(968904582), "Read operation for symbol '{SymbolPath}' was canceled.", symbolPath);
                    tcs.SetCanceled(linkedCts.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(new EventId(968904583), ex, "Failed to read value for symbol '{SymbolPath}'.", symbolPath);
                    tcs.SetException(ex);
                }
            };

            await _adsOperationChannel.Writer.WriteAsync(readOperation, token);
            return await tcs.Task;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(new EventId(968904582), "Read operation for symbol '{SymbolPath}' was canceled.", symbolPath);
            throw new TaskCanceledException("The read operation was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(new EventId(968904583), ex, "Failed to queue read operation for symbol '{SymbolPath}'.", symbolPath);
            throw new InvalidOperationException($"Failed to queue read operation for symbol '{symbolPath}'.", ex);
        }
    }

    public async Task<ResultHandle> AddDeviceNotificationAsync(string symbolPath, int dataSize, NotificationSettings settings, object? userData, CancellationToken token)
    {
        ObjectDisposedException.ThrowIf(_disposed, nameof(AdsService));

        if (string.IsNullOrWhiteSpace(symbolPath))
            throw new ArgumentException("Symbol path cannot be null or whitespace.", nameof(symbolPath));

        var tcs = new TaskCompletionSource<ResultHandle>(TaskCreationOptions.RunContinuationsAsynchronously);

        Func<AdsClient, Task> operation = async (adsClient) =>
        {
            try
            {
                var result = await adsClient.AddDeviceNotificationAsync(symbolPath, dataSize, settings, userData, token);
                tcs.SetResult(result);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        };

        await _adsOperationChannel.Writer.WriteAsync(operation, token);
        return await tcs.Task;
    }

    private async Task ProcessAdsOperationsAsync(CancellationToken token)
    {
        try
        {
            await foreach (var operation in _adsOperationChannel.Reader.ReadAllAsync(token))
            {
                try
                {
                    await operation(_adsClient);
                }
                catch (Exception ex)
                {
                    _logger.LogError(new EventId(968904578), ex, "Unexpected error processing ADS operation.");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("ADS processing task was canceled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(new EventId(968904578), ex, "Critical error in ADS processing task.");
        }
    }

    private async void CheckHeartbeat(object? state)
    {
        if (_disposed || _processingCts.Token.IsCancellationRequested)
            return;

        try
        {
            // Den Heartbeat-Wert über die öffentliche, asynchrone Methode lesen.
            // Diese Methode reiht die Operation bereits in die Queue ein.
            var heartbeat = await ReadValueAsync<ulong>("GVL_HV.Heartbeat", _processingCts.Token);

            // Hier wird die Heartbeat-Logik ausgeführt, sobald das Ergebnis vorliegt.
            if (_lastHeartbeat == null ||
                heartbeat < _lastHeartbeat ||
                (heartbeat > _lastHeartbeat + 20_000_000))
            {
                _logger.LogInformation(new EventId(1987762785),
                    "PLC restart detected! Current: {Heartbeat}, Last: {LastHeartbeat}",
                    heartbeat, _lastHeartbeat);

                if (!_firstHeartbeatRun)
                {
                    _plcRestartDetected.OnNext(_lastHeartbeat);
                }
                else
                {
                    _firstHeartbeatRun = false;
                }
            }
            else
            {
                _logger.LogDebug(new EventId(902294163),
                    "Heartbeat OK - Current: {Heartbeat}, Diff: {Diff}ns",
                    heartbeat, heartbeat - _lastHeartbeat);
            }

            _lastHeartbeat = heartbeat;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(new EventId(1708850059), "Heartbeat read operation was canceled.");
        }
        catch (Exception ex)
        {
            // Fehler werden bereits in ReadValueAsync protokolliert, aber wir behandeln sie hier ebenfalls,
            // um den Heartbeat-Status zu aktualisieren und ein Event zu senden.
            _logger.LogError(new EventId(744893936), ex, "Error reading heartbeat");
            _lastHeartbeat = null;
            _plcRestartDetected.OnNext(null);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                _heartbeatTimer?.Dispose();
                _adsOperationChannel.Writer.Complete();
                _processingCts.Cancel();

                if (!_adsProcessingTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    _logger.LogWarning(new EventId(968904579), "ADS processing task did not complete within timeout.");
                }

                _plcRestartDetected?.OnCompleted();
                _plcRestartDetected?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(new EventId(968904580), ex, "Error during disposal.");
            }
            finally
            {
                _processingCts.Dispose();
                _adsClient.AdsNotification -= OnAdsNotification; // Event-Abonnement aufheben
                _adsClient.Dispose();
                _disposed = true;
            }
        }
    }
}