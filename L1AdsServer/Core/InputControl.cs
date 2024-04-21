using L1AdsServer.Core.NewFolder;
using System.Text.Json;
using TwinCAT.Ads;

namespace L1AdsServer.Core;

public class InputControl : IInputControl
{
    private readonly ILogger<InputControl> _logger;
    private readonly IDataExtractor _dataExtractor;

    private readonly AdsClient _adsClient;

    private readonly string _bearerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJjMDkyNmVjMjFiODY0ODlhOWI5M2FkMDMwODcwMzZlZSIsImlhdCI6MTcxMjYxMDk5NiwiZXhwIjoyMDI3OTcwOTk2fQ.dg2LexXQ6B5Djz1OT17F0h6PXD9xQ-2LoZGDpOE_JSY";

    public InputControl(ILogger<InputControl> logger, IDataExtractor dataExtractor)
    {
        _logger = logger;
        _dataExtractor = dataExtractor;

        _adsClient = new AdsClient();
        _adsClient.Connect(AmsNetId.Local, 851);

        _adsClient.AdsNotification += OnNotification;
    }

    private void OnNotification(object? sender, AdsNotificationEventArgs e)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + _bearerToken);

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "http://hal1:8123/api/states/sensor.eg_main_door_locked");
            request.Content = new StringContent(JsonSerializer.Serialize(new { state = e.Data.ToArray()[0] == 1 ? "true" : "false" }));
            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = client.Send(request);
            _logger.LogWarning("Response", response);
        }


        _logger.LogWarning($"OnNotification {e.UserData}");
    }

    public async Task<bool> GetAsync(InputId id, CancellationToken token)
    {            
        var variableName = _dataExtractor.CreateVariableName(id.ToString(), "Input", out bool firstAccess, out VariableInfo info);
        if (firstAccess)
            await RegisterChangeDetectionAsync(variableName, info, token);

        var resultValue =  await _adsClient.ReadValueAsync<bool>(variableName, token);
        resultValue.ThrowOnError();
        return resultValue.Value;
    }

    private async Task RegisterChangeDetectionAsync(string variableName, VariableInfo info, CancellationToken token)
    {
        await _adsClient.AddDeviceNotificationAsync(variableName, 1, new NotificationSettings(AdsTransMode.OnChange, 100, 0), info, token);
    }
}
