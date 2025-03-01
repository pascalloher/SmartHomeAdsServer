using System.Diagnostics.Eventing.Reader;
using System.Reactive.Subjects;
using TwinCAT.Ads;

namespace L1AdsServer.Core.Plc
{
    public class HeartbeatMonitor : IHeartbeatMonitor, IDisposable
    {
        private ulong? _lastHeartbeat;
        private bool _firstRun = true;

        private readonly ILogger<HeartbeatMonitor> _logger;
        private readonly Timer _timer;
        private readonly Subject<ulong?> _plcRestartDetected;

        public IObservable<ulong?> PlcRestartDetected => _plcRestartDetected;

        public HeartbeatMonitor(ILogger<HeartbeatMonitor> logger)
        {
            _lastHeartbeat = null;

            _logger = logger;
            _plcRestartDetected = new Subject<ulong?>();
            _timer = new Timer(CheckHeartbeat, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
        }

        private void CheckHeartbeat(object? state)
        {
            try
            {
                using var adsClient = new AdsClient();
                adsClient.Connect(AmsNetId.Local, 851);
                // Heartbeat gets incremented by 100ns Steps
                var readResult = adsClient.TryReadValue<ulong>("GVL_HV.Heartbeat", out ulong heartbeat);
                if (readResult != AdsErrorCode.NoError)
                {
                    _logger.LogError(new EventId(1708850058), "Error reading heartbeat: AdsErrorCode {AdsErrorCode}", readResult);
                    _lastHeartbeat = null;
                    return;
                }
                if(_lastHeartbeat == null || (heartbeat <= _lastHeartbeat) || (heartbeat > _lastHeartbeat + 20_000_000))
                {
                    _logger.LogInformation(new EventId(1987762785), "Checking plc heartbeat -> restart detected! AdsErrorCode: {AdsErrorCode}, Heartbeat: {Heartbeat}", readResult.ToString(), heartbeat);
                    if(_firstRun != true)
                        _plcRestartDetected.OnNext(_lastHeartbeat);
                    else
                        _firstRun = false;
                }
                else
                {
                    _logger.LogInformation(new EventId(902294163), "Checking plc heartbeat, Heartbeat: {Heartbeat}, Diff: {Diff}", heartbeat, heartbeat - _lastHeartbeat);
                }
                _lastHeartbeat = heartbeat;
            }
            catch(Exception ex)
            {
                // Log the exception
                _logger.LogError(new EventId(744893936), ex, "Error reading heartbeat");
                _plcRestartDetected.OnNext(_lastHeartbeat);
            }
        }

        public void Dispose()
        {
            _timer?.Dispose();
            _plcRestartDetected?.Dispose();
        }
    }
}
