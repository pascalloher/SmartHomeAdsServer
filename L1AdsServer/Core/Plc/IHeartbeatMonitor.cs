namespace L1AdsServer.Core.Plc
{
    public interface IHeartbeatMonitor
    {
        IObservable<ulong?> PlcRestartDetected { get; }
    }
}
