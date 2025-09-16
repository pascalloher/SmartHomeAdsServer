using System.Diagnostics.CodeAnalysis;
using System.Threading.Channels;
using TwinCAT.Ads;

namespace L1AdsServer.Core.Plc;

public sealed class AdsService : IAdsService
{
	private readonly ILogger<AdsService> _logger;
	private readonly ILoggerFactory _loggerFactory;
    private readonly IPlcHeartbeatMonitor _heartbeatMonitor;
    private AdsClient? _adsClient;
	private Channel<Func<AdsClient, Task>>? _adsOperationChannel;
	private Task? _adsProcessingTask;
	private CancellationTokenSource? _processingCts;
	
	private bool _disposed;

	public event EventHandler<AdsNotificationEventArgs>? AdsNotification;
	
	public AdsService(ILoggerFactory loggerFactory)
	{
		_loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<AdsService>();
        Initialize();

        _heartbeatMonitor = new PlcHeartbeatMonitor(_loggerFactory.CreateLogger<PlcHeartbeatMonitor>(), this);
        _heartbeatMonitor.PlcRestartDetected.Subscribe(_ =>
        {
            _logger.LogWarning(new EventId(1458431755), "HeartbeatMonitor.PlcRestartDetected -> calling ReConnect()");
            (this as IAdsService).ReConnect();
        });
    }
	
	private void Initialize()
	{
		_disposed = false;
        _adsClient = new AdsClient();
		_adsClient.AdsNotification += OnAdsNotification;
		_adsClient.Connect(new AmsAddress(AmsNetId.Local, 851));
		
		_adsOperationChannel = Channel.CreateBounded<Func<AdsClient, Task>>(new BoundedChannelOptions(200)
		{
			SingleReader = true,
			SingleWriter = false,
			FullMode = BoundedChannelFullMode.DropOldest,
		});
		
		_processingCts = new CancellationTokenSource();
		_adsProcessingTask = Task.Run(() => ProcessAdsOperationsAsync(_processingCts.Token));
    }

	private void Dispose(bool disposing)
	{
		if(_disposed || !disposing)
			return;
		
		try
		{
            if (_adsOperationChannel != null)
			{
				_adsOperationChannel.Writer.Complete();
				while (_adsOperationChannel.Reader.TryRead(out var _))
				{
				}
			}

			if(_adsProcessingTask != null)
			{
				_processingCts?.Cancel();
				if(!_adsProcessingTask.Wait(TimeSpan.FromSeconds(5)))
				{
					_logger.LogWarning(new EventId(968904579), "ADS processing task did not complete within timeout.");
				}
			}

			if(_adsClient == null)
				return;
			_adsClient.AdsNotification -= OnAdsNotification;
			_adsClient.Disconnect();
		}
		catch (Exception ex)
		{
			_logger.LogError(new EventId(968904580), ex, "Error during disposal.");
		}
		finally
		{
			_adsClient?.Dispose();
			_adsProcessingTask = null;
			
			_processingCts?.Dispose();
			_disposed = true;
		}
	}
	
	void IAdsService.ReConnect()
	{
		Dispose();
		Initialize();
	}

	private void OnAdsNotification(object? sender, AdsNotificationEventArgs e)
	{
		AdsNotification?.Invoke(this, e);
	}

	async Task<ResultWrite> IAdsService.WriteValueAsync<T>(string symbolPath, [DisallowNull] T value, CancellationToken token)
	{
		ObjectDisposedException.ThrowIf(_disposed, nameof(AdsService));
		if (string.IsNullOrWhiteSpace(symbolPath)) throw new ArgumentException("Symbol path cannot be null or whitespace.", nameof(symbolPath));
		if (value is null)
		{
			_logger.LogError(new EventId(968904573), "Attempted to write a null value for symbol '{SymbolPath}'.", symbolPath);
			throw new ArgumentNullException(nameof(value), "Value cannot be null.");
		}
		try
		{
			var tcs = new TaskCompletionSource<ResultWrite>(TaskCreationOptions.RunContinuationsAsynchronously);
			if(_processingCts == null)
				throw new InvalidOperationException("The ADS processing cancellation token source is not initialized.");
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _processingCts.Token);

			if(_adsOperationChannel == null)
				throw new InvalidOperationException("The ADS operation channel is not initialized.");
			await _adsOperationChannel.Writer.WriteAsync(WriteOperation, linkedCts.Token);
			return await tcs.Task;

			async Task WriteOperation(AdsClient adsClient)
			{
				try
				{
					var result = await adsClient.WriteValueAsync(symbolPath, value, _processingCts.Token);
					tcs.SetResult(result);
				}
				catch (OperationCanceledException)
				{
					_logger.LogWarning(new EventId(968904575), "Write operation for symbol '{SymbolPath}' was canceled.", symbolPath);
					tcs.SetCanceled(_processingCts.Token);
				}
				catch (Exception ex)
				{
					_logger.LogError(new EventId(968904577), ex, "Failed to write value for symbol '{SymbolPath}'.", symbolPath);
					tcs.SetException(ex);
				}
			}
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

	async Task<T> IAdsService.ReadValueAsync<T>(string symbolPath, CancellationToken token)
	{
		ObjectDisposedException.ThrowIf(_disposed, nameof(AdsService));
		if (string.IsNullOrWhiteSpace(symbolPath)) throw new ArgumentException("Symbol path cannot be null or whitespace.", nameof(symbolPath));
		try
		{
			var tcs = new TaskCompletionSource<T?>(TaskCreationOptions.RunContinuationsAsynchronously);
			if(_processingCts == null)
				throw new InvalidOperationException("The ADS processing cancellation token source is not initialized.");
			using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, _processingCts.Token);
			if(_adsOperationChannel == null)
				throw new InvalidOperationException("The ADS operation channel is not initialized.");
			await _adsOperationChannel.Writer.WriteAsync(ReadOperation, linkedCts.Token);
			return await tcs.Task ?? throw new InvalidOperationException($"Read operation for symbol '{symbolPath}' returned null.");

			async Task ReadOperation(AdsClient adsClient)
			{
				try
				{
					var result = await adsClient.ReadValueAsync<T>(symbolPath, _processingCts.Token);
					tcs.SetResult(result.Value);
				}
				catch (OperationCanceledException)
				{
					_logger.LogWarning(new EventId(968904582), "Read operation for symbol '{SymbolPath}' was canceled.", symbolPath);
					tcs.SetCanceled(_processingCts.Token);
				}
				catch (Exception ex)
				{
					_logger.LogError(new EventId(968904583), ex, "Failed to read value for symbol '{SymbolPath}'.", symbolPath);
					tcs.SetException(ex);
				}
			}
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

	async Task<ResultHandle> IAdsService.AddDeviceNotificationAsync(string symbolPath, int dataSize, NotificationSettings settings, object? userData, CancellationToken token)
	{
		ObjectDisposedException.ThrowIf(_disposed, nameof(AdsService));
		if (string.IsNullOrWhiteSpace(symbolPath)) throw new ArgumentException("Symbol path cannot be null or whitespace.", nameof(symbolPath));
		var tcs = new TaskCompletionSource<ResultHandle>(TaskCreationOptions.RunContinuationsAsynchronously);

		if(_adsOperationChannel == null)
			throw new InvalidOperationException("The ADS operation channel is not initialized.");
		await _adsOperationChannel.Writer.WriteAsync(Operation, token);
		return await tcs.Task;

		async Task Operation(AdsClient adsClient)
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
		}
	}

	private async Task ProcessAdsOperationsAsync(CancellationToken token)
	{
		try
		{
			if(_adsOperationChannel == null)
				throw new InvalidOperationException("The ADS operation channel is not initialized.");
			await foreach (var operation in _adsOperationChannel.Reader.ReadAllAsync(token))
			{
				try
				{
					if(_adsClient == null)
						throw new InvalidOperationException("The ADS client is not initialized.");
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

	public void Dispose()
	{
		Dispose(true);
		
		GC.SuppressFinalize(this);
	}
}