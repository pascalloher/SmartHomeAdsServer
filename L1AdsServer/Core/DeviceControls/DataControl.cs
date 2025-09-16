using System.Collections.Concurrent;
using System.Runtime.InteropServices;
using System.Text.Json;
using L1AdsServer.Controllers;
using L1AdsServer.Core.Plc;
using TwinCAT.Ads;

namespace L1AdsServer.Core.Controls;

[StructLayout(LayoutKind.Sequential, Pack = 0, CharSet = CharSet.Ansi)]
public struct StPower
{
    // Voltage: ARRAY[1..3] OF REAL;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] Voltage;

    // Current: ARRAY[1..3] OF REAL;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] Current;

    // ActivePower : ARRAY[1..4] OF REAL;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] ActivePower;

    // ApparentPower: ARRAY[1..3] OF REAL;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] ApparentPower;

    // ReactivePower: ARRAY[1..3] OF REAL;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] ReactivePower;

    // PowerFactor: ARRAY[1..4] OF REAL;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public float[] PowerFactor;

    // ActiveEnergy: ARRAY[1..4] OF LINT;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public long[] ActiveEnergy;

    // ApparentEnergy: ARRAY[1..3] OF LINT;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public long[] ApparentEnergy;

    // ReactiveEnergy: ARRAY[1..3] OF LINT;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public long[] ReactiveEnergy;

    // ActivePositiveEnergy: LINT;
    public long ActivePositiveEnergy;

    // ActiveNegativeEnergy: LINT;
    public long ActiveNegativeEnergy;

    // Frequency: REAL;
    public float Frequency;

    // CalculatedNeutralLineCurrent: REAL;
    public float CalculatedNeutralLineCurrent;
}

public class DataControl : IDataControl
{
    private readonly ILogger<DataControl> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAdsService _adsService;
    private readonly ConcurrentDictionary<string, DataDescription> _dataDescriptions;

    public DataControl(ILogger<DataControl> logger, IHttpClientFactory httpClientFactory, IAdsService adsService)
    {
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _adsService = adsService;
        _dataDescriptions = new ConcurrentDictionary<string, DataDescription>();

        // Subscribe to the HeartbeatMonitor
        /*_adsService.PlcRestartDetected.Subscribe(async heartbeat => {
            await OnPlcRestartDetectedAsync(heartbeat);
        });*/
    }

    private async Task OnPlcRestartDetectedAsync(ulong? lastHeartbeat)
    {
        _logger.LogWarning(new EventId(123456789), "PLC restart detected. Last heartbeat: {LastHeartbeat}", lastHeartbeat);

        foreach(var dataDescription in _dataDescriptions.Values)
        {
            if(dataDescription.RegisterChangeDetection == true)
            {
                await RegisterChangeDetectionAsync(dataDescription, CancellationToken.None);
            }
        }
    }

    public async Task<object?> SubscribeAsync(DataDescription dataDescription, CancellationToken token)
    {
        if(dataDescription.RegisterChangeDetection && _dataDescriptions.TryAdd(dataDescription.PlcName, dataDescription))
            await RegisterChangeDetectionAsync(dataDescription, token);

        switch(dataDescription.Type)
        {
            case "StPower":
                var power = await _adsService.ReadValueAsync<StPower>(dataDescription.PlcName, token);
                return JsonSerializer.Serialize(power, new JsonSerializerOptions { IncludeFields = true });
            default:
                break;
        }
        return null;
    }

    private async Task RegisterChangeDetectionAsync(DataDescription dataDescription, CancellationToken token)
    {
        dataDescription.PlcHandle = await _adsService.AddDeviceNotificationAsync(dataDescription.PlcName,
            GetDataSize(dataDescription.Type), new NotificationSettings(AdsTransMode.OnChange, 100, 0),
            dataDescription, token);
    }

    private static int GetDataSize(string type)
    {
        return type switch
        {
            "StPower" => Marshal.SizeOf<StPower>(),
            _ => throw new ArgumentException(type)
        };
    }

    private void OnNotification(object? sender, AdsNotificationEventArgs e)
    {
        if (e.UserData is DataDescription description)
        {
            object d = description.Type switch
            {
                "StPower" => d = e.Data.ToArray().ReadUsingMarshalSafe<StPower>(),
                _ => throw new ArgumentException(description.Type)
            };
            var data = JsonSerializer.Serialize(new { state = d }, new JsonSerializerOptions
            {

                IncludeFields = true
            });
            SendReply(description, data);
        }
    }

    private void SendReply(DataDescription description, string data)
    {
        using var client = _httpClientFactory.CreateClient(nameof(DataControl));
        var request = new HttpRequestMessage(HttpMethod.Post, $"states/sensor.{description.HaName}");
        request.Content = new StringContent(data);
        request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        var response = client.Send(request);
        _logger.LogInformation(new EventId(1876179047), "Post Value Response: {Response}", response);
    }
}

public static class Extensions
{
    public static T? ReadUsingMarshalSafe<T>(this byte[] data)
    {
        if (data is not { Length: > 0 }) return default;
        var gch = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            if (Marshal.PtrToStructure(gch.AddrOfPinnedObject(), typeof(T)) is T structure)
                return structure;
            return default;
        }
        finally
        {
            gch.Free();
        }
    }
}
