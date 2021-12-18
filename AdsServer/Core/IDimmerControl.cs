namespace AdsServer1.Core
{
    public interface IDimmerControl
    {
        Task SetAsync(int dimmer, uint value, CancellationToken token);
        Task<uint> GetAsync(int dimmer, CancellationToken token);
    }
}
