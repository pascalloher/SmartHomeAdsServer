using TwinCAT.Ads;

namespace L1AdsServer.Core;

public class SwitchControl : ISwitchControl
{
    private readonly ILogger<SwitchControl> _logger;

    private readonly AdsClient _adsClient;

    public SwitchControl(ILogger<SwitchControl> logger)
    {
        _logger = logger;

        _adsClient = new AdsClient();
        _adsClient.Connect(AmsNetId.Local, 851);
    }

    public async Task OnAsync(SwitchId id, CancellationToken token)
    {
        if(GetFloor(id) == Floor.Ug)
            await SetValueOnPlcAsync(GetFloor(id), GetNumber(id), 1, token);
        else
            await SetValueOnPlcAsync(GetFloor(id), (GetNumber(id) * 2) - 1, 1, token);
    }

    public async Task OffAsync(SwitchId id, CancellationToken token)
    {
        if (GetFloor(id) == Floor.Ug)
            await SetValueOnPlcAsync(GetFloor(id), GetNumber(id), 0, token);
        else
            await SetValueOnPlcAsync(GetFloor(id), (GetNumber(id) * 2) - 1, 0, token);
    }

    private async Task SetValueOnPlcAsync(Floor floor, int channel, int value, CancellationToken token)
    {
        string variableName = string.Empty;
        if(floor == Floor.Ug)
            variableName = $"GVL_HV.{floor}Out[{channel}]";
        else
            variableName = $"GVL_UV.{floor}Out[{channel}]";
        var result = await _adsClient.WriteValueAsync(variableName, value, token);
        result.ThrowOnError();
    }

    private static Floor GetFloor(SwitchId id)
    {
        string floorString = id.ToString()[..2];

        if (Enum.TryParse(floorString, out Floor floor))
        {
            return floor;
        }
        else
        {
            throw new ArgumentException($"Invalid SwitchId '{id}'");
        }
    }

    private static int GetNumber(SwitchId id)
    {
        // Assuming the numeric part starts from the third character
        if (int.TryParse(id.ToString()[2..], out int number))
        {
            return number;
        }
        else
        {
            // Handle the case where the numeric part is not a valid integer
            throw new ArgumentException($"Invalid SwitchId '{id}'");
        }
    }
}
