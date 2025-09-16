using TwinCAT.Ads;

namespace L1AdsServer.Core.Plc;

public class ReadRequest<T>
{
    public string SymbolPath { get; }
    public TaskCompletionSource<T> Tcs { get; }
    public CancellationToken Token { get; }

    public ReadRequest(string symbolPath, TaskCompletionSource<T> tcs, CancellationToken token)
    {
        SymbolPath = symbolPath;
        Tcs = tcs;
        Token = token;
    }
}

public interface IReadRequest
{
    string SymbolPath { get; }
    CancellationToken Token { get; }
    Task ProcessAsync(AdsClient adsClient);
}

public class TypedReadRequest<T> : IReadRequest
{
    public string SymbolPath { get; }
    public TaskCompletionSource<T?> Tcs { get; }
    public CancellationToken Token { get; }

    public TypedReadRequest(string symbolPath, TaskCompletionSource<T?> tcs, CancellationToken token)
    {
        SymbolPath = symbolPath;
        Tcs = tcs;
        Token = token;
    }

    public async Task ProcessAsync(AdsClient adsClient)
    {
        try
        {
            var result = await adsClient.ReadValueAsync<T>(SymbolPath, Token);
            Tcs.SetResult(result.Value);
        }
        catch (OperationCanceledException)
        {
            Tcs.SetCanceled(Token);
        }
        catch (Exception ex)
        {
            Tcs.SetException(ex);
        }
    }
}
