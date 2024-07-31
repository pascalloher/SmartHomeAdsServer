namespace L1AdsServer.Configuration;

public class HomeAssistantConfig(Uri uri, string bearerToken)
{
    public Uri Uri { get; } = uri;
    public string BearerToken { get; } = bearerToken;
}