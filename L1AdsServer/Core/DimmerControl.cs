using TwinCAT.Ads;

namespace L1AdsServer.Core;

public class DimmerControl : IDimmerControl
{
    private Dictionary<DimmerId, ushort> _dimmerValues;
    private readonly AdsClient _adsClient;

    public DimmerControl()
    {
        _dimmerValues = new Dictionary<DimmerId, ushort>(Enum.GetValues<DimmerId>().Length);
        _adsClient = new AdsClient();
        _adsClient.Connect(AmsNetId.Local, 851);
    }

    public async Task<uint> GetAsync(int dimmer, CancellationToken token)
    {
        ResultValue<uint> readResult = await _adsClient.ReadValueAsync<uint>($"Main.Dimmer[{dimmer}]", token);
        readResult.ThrowOnError();
        return readResult.Value;
    }

    public async Task OnAsync(DimmerId id, CancellationToken token)
    {
        await SetOnPlcAsync(id, _dimmerValues.GetValueOrDefault(id), token);
    }

    public async Task OffAsync(DimmerId id, CancellationToken token)
    {
        await SetOnPlcAsync(id, 0, token);
    }

    public async Task SetAsync(DimmerId id, ushort value, CancellationToken token)
    {
        _dimmerValues[id] = value;
        await SetOnPlcAsync(id, value, token);
    }

    private async Task SetOnPlcAsync(DimmerId id, ushort value, CancellationToken token)
    {
        var variableName = $"GVL_UV.{GetFloor(id)}Dimmer[{GetNumber(id)}]";
        var result = await _adsClient.WriteValueAsync(variableName, value, token);
        result.ThrowOnError();
    }

    private static Floor GetFloor(DimmerId id)
    {
        string floorString = id.ToString()[..2];

        if (Enum.TryParse(floorString, out Floor floor))
        {
            return floor;
        }
        else
        {
            throw new ArgumentException($"Invalid DimmerId '{id}'");
        }
    }

    private static int GetNumber(DimmerId id)
    {
        // Assuming the numeric part starts from the third character
        if (int.TryParse(id.ToString()[2..], out int number))
        {
            return number;
        }
        else
        {
            // Handle the case where the numeric part is not a valid integer
            throw new ArgumentException($"Invalid DimmerId '{id}'");
        }
    }
}
