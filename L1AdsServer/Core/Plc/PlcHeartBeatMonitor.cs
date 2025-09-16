using System.Drawing.Text;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace L1AdsServer.Core.Plc;

enum HeartbeatStatus
{
    FirstRun,
    Running,
    RestartDetected
}

public class PlcHeartbeatMonitor : IPlcHeartbeatMonitor, IDisposable
{
    private readonly ILogger<PlcHeartbeatMonitor> _logger;
    private readonly IAdsService _adsService;
    private readonly bool _debugMode = false;
    private bool _requestInProgress = false;
    private DateTimeOffset _restartTime;
    
    private readonly Subject<ulong?> _plcRestartDetected = new();
    private readonly Timer _heartbeatTimer;
    private ulong? _lastHeartbeat;
    private HeartbeatStatus _heartbeatStatus = HeartbeatStatus.FirstRun;
    private bool _disposed;

    public IObservable<ulong?> PlcRestartDetected => _plcRestartDetected.AsObservable();

    public PlcHeartbeatMonitor(ILogger<PlcHeartbeatMonitor> logger, IAdsService adsService)
    {
        _logger = logger;
        _adsService = adsService;
        _restartTime = DateTimeOffset.Now;

        _heartbeatTimer = new Timer(CheckHeartbeat, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1));
    }

    private async void CheckHeartbeat(object? state)
    {
        if (_disposed || _requestInProgress)
            return;

        _requestInProgress = true;

        bool success = false;
        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.9));
            var heartbeat = await _adsService.ReadValueAsync<ulong>("GVL_HV.Heartbeat", cts.Token);
            success = true;
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error reading heartbeat");
        }
        finally
        {
            _requestInProgress = false;
        }


            switch (_heartbeatStatus)
            {
                case HeartbeatStatus.FirstRun:
                    _logger.LogInformation("Initial heartbeat read: {Heartbeat}", heartbeat);
                    _heartbeatStatus = HeartbeatStatus.Running;
                    break;
                case HeartbeatStatus.Running:
                    if(heartbeat == 0)
                    {
                        if (DateTimeOffset.Now - _restartTime > TimeSpan.FromSeconds(20))
                        {
                            _logger.LogWarning("PLC hearbeat stays at 0!");
                            _heartbeatTimer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
                            _heartbeatStatus = HeartbeatStatus.RestartDetected;
                            _restartTime = DateTimeOffset.Now;
                            _plcRestartDetected.OnNext(_lastHeartbeat);
                        }
                    }

                    if ((heartbeat <= _lastHeartbeat || (heartbeat > _lastHeartbeat + 20_000_000)) && !_debugMode)
                    {
                        _logger.LogWarning("PLC restart detected! Current: {Heartbeat}, Last: {LastHeartbeat}", heartbeat, _lastHeartbeat);
                        _heartbeatTimer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
                        _heartbeatStatus = HeartbeatStatus.RestartDetected;
                        _restartTime = DateTimeOffset.Now;
                        _plcRestartDetected.OnNext(_lastHeartbeat);
                    }
                    else
                    {
                        _logger.LogInformation("Heartbeat OK - Current: {Heartbeat}, Diff: {Diff}ns", heartbeat, heartbeat - _lastHeartbeat);
                    }
                    break;
                case HeartbeatStatus.RestartDetected:
                    if(heartbeat != _lastHeartbeat)
                    {
                        _logger.LogInformation("PLC heartbeat running again: {Heartbeat}", heartbeat);
                        _heartbeatTimer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(1));
                        _heartbeatStatus = HeartbeatStatus.Running;
                    }
                    break;
            }
            _lastHeartbeat = heartbeat;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _heartbeatTimer.Dispose();
        _plcRestartDetected.OnCompleted();
        _plcRestartDetected.Dispose();
        _disposed = true;
    }

    public void ReportRestart()
    {
        throw new NotImplementedException();
    }
}