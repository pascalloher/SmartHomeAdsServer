namespace L1AdsServer.Core.Plc;

public interface IPlcHeartbeatMonitor
{
    IObservable<ulong?> PlcRestartDetected { get; }

    void ReportRestart();
}
