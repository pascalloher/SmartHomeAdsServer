using TwinCAT.Ads;

namespace L1AdsServer.Core;

public class BlindControl : IBlindControl
{
    private readonly AdsClient _adsClient;

    public BlindControl()
    {
        _adsClient = new AdsClient();
        _adsClient.Connect(AmsNetId.Local, 851);
    }

    public async Task OpenAsync(BlindId id, CancellationToken token)
    {
        await SetCloseOnPlcAsync(id, false, token);
        await Task.Delay(500);
        await SetOpenOnPlcAsync(id, true, token);
    }

    public async Task CloseAsync(BlindId id, CancellationToken token)
    {
        await SetOpenOnPlcAsync(id, false, token);
        await Task.Delay(500);
        await SetCloseOnPlcAsync(id, true, token);
    }

    public async Task StopAsync(BlindId id, CancellationToken token)
    {
        await SetOpenOnPlcAsync(id, false, token);
        await SetCloseOnPlcAsync(id, false, token);
    }

    private async Task SetOpenOnPlcAsync(BlindId id, bool value, CancellationToken token)
    {
        var variableName = $"GVL_{GetFloor(id)}.BlindOpen[{GetNumber(id)}]";
        var result = await _adsClient.WriteValueAsync(variableName, value, token);
        result.ThrowOnError();
    }

    private async Task SetCloseOnPlcAsync(BlindId id, bool value, CancellationToken token)
    {
        var variableName = $"GVL_{GetFloor(id)}.BlindClose[{GetNumber(id)}]";
        var result = await _adsClient.WriteValueAsync(variableName, value, token);
        result.ThrowOnError();
    }

    private static Floor GetFloor(BlindId id)
    {
        string floorString = id.ToString()[..2];

        if (Enum.TryParse(floorString, out Floor floor))
        {
            return floor;
        }
        else
        {
            throw new ArgumentException($"Invalid BlindId '{id}'");
        }
    }

    private static int GetNumber(BlindId id)
    {
        // Assuming the numeric part starts from the third character
        if (int.TryParse(id.ToString()[2..], out int number))
        {
            return number;
        }
        else
        {
            // Handle the case where the numeric part is not a valid integer
            throw new ArgumentException($"Invalid BlindId '{id}'");
        }
    }
}
