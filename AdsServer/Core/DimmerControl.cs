using TwinCAT.Ads;

namespace AdsServer1.Core
{
    public class DimmerControl : IDimmerControl
    {
        public async Task SetAsync(int dimmer, uint value, CancellationToken token)
        {
            using AdsClient client = new();
            client.Connect(AmsNetId.Local, 851);
            await client.WriteValueAsync($"Main.Dimmer[{dimmer}]", value, token);
        }

        public async Task<uint> GetAsync(int dimmer, CancellationToken token)
        {
            using AdsClient client = new();
            client.Connect(AmsNetId.Local, 851);
            ResultValue<uint> readResult = await client.ReadValueAsync<uint>($"Main.Dimmer[{dimmer}]", token);
            readResult.ThrowOnError();
            return readResult.Value;
        }
    }
}
