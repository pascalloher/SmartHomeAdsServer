using L1AdsServer.Core.NewFolder;
using TwinCAT.Ads;

namespace L1AdsServer.Core;

public enum BlindState
{
    Unknown = 0,
    Closed = 1,
    Opening = 2,
    Open = 3,
    Closing = 4,
    StoppedInBetween = 5,
}

public class Blind
{

    private ILogger<Blind> _logger;
    private readonly AdsClient _adsClient;

    private string _openVariableName;
    private string _closeVariableName;

    private System.Timers.Timer _openTimer;
    private System.Timers.Timer _closeTimer;

    private readonly TimeSpan _switchDirectionTimeout;

    private BlindState _state;

    public BlindId Id { get; init; }

    public TimeSpan _openTime { get; init; }
    public TimeSpan _closeTime { get; init; }
    public BlindState State {
        get
        {
            return _state;
        }
        set
        {
            _logger.LogWarning("Blind: {Id} -> {State}", Id, value);
            _state = value;
        }
    }

    public double Position { get; set; }

    public Blind(ILogger<Blind> logger, BlindId id, TimeSpan openTime, TimeSpan closeTime, string openVariableName, string closeVariableName, AdsClient adsClient)
    {
        _logger = logger;

        Id = id;
        State = BlindState.Unknown;
        _openTime = openTime;
        _closeTime = closeTime;
        _switchDirectionTimeout = TimeSpan.FromSeconds(2);
        _openVariableName = openVariableName;
        _closeVariableName = closeVariableName;
        _adsClient = adsClient;

        _openTimer = new System.Timers.Timer(openTime)
        {
            AutoReset = false
        };
        _openTimer.Elapsed += OnOpenTimerElapsed;

        _closeTimer = new System.Timers.Timer(closeTime)
        {
            AutoReset = false
        };
        _closeTimer.Elapsed += OnCloseTimerElapsed;
    }

    private async void OnOpenTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        await SetOpenOnPlcAsync(false, CancellationToken.None);
        State = BlindState.Open;
    }

    private async void OnCloseTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        await SetCloseOnPlcAsync(false, CancellationToken.None);
        State = BlindState.Closed;
    }

    public async Task CloseAsync(CancellationToken token)
    {
        if(State == BlindState.Opening)
        {
            await SetOpenOnPlcAsync(false, token);
            State = BlindState.StoppedInBetween;
            await Task.Delay(_switchDirectionTimeout);
        }
        await SetCloseOnPlcAsync(true, token);
        State = BlindState.Closing;

        _closeTimer.Start();
    }

    public async Task OpenAsync(CancellationToken token)
    {
        if(State == BlindState.Closing)
        {
            await SetCloseOnPlcAsync(false, token);
            State = BlindState.StoppedInBetween;
            await Task.Delay(_switchDirectionTimeout);
        }
        await SetOpenOnPlcAsync(true, token);
        State = BlindState.Opening;

        _openTimer.Start();
    }

    public async Task StopAsync(CancellationToken token)
    {
        if(State == BlindState.Closing)
        {
            _closeTimer.Stop();
            await SetCloseOnPlcAsync(false, token);
            State = BlindState.StoppedInBetween;
        }
        else if(State == BlindState.Opening)
        {
            _openTimer.Stop();
            await SetOpenOnPlcAsync(false, token);
            State = BlindState.StoppedInBetween;
        }        
    }

    private async Task SetOpenOnPlcAsync(bool value, CancellationToken token)
    {
        var result = await _adsClient.WriteValueAsync(_openVariableName, value, token);
        result.ThrowOnError();
    }

    private async Task SetCloseOnPlcAsync(bool value, CancellationToken token)
    {
        var result = await _adsClient.WriteValueAsync(_closeVariableName, value, token);
        result.ThrowOnError();
    }
}

public class BlindControl : IBlindControl
{
    private readonly IDataExtractor _dataExtractor;
    private readonly AdsClient _adsClient;


    private readonly IDictionary<BlindId, Blind> _blinds;

    public BlindControl(ILoggerFactory loggerFactory, IDataExtractor dataExtractor)
    {
        _dataExtractor = dataExtractor;

        _adsClient = new AdsClient();
        _adsClient.Connect(AmsNetId.Local, 851);

        _blinds = new Dictionary<BlindId, Blind>();
        foreach(var id in Enum.GetValues<BlindId>())
        {
            string openVariableName = _dataExtractor.CreateVariableName(id.ToString(), "BlindOpen", out bool _, out VariableInfo _);
            string closeVariableName = _dataExtractor.CreateVariableName(id.ToString(), "BlindClose", out bool _, out VariableInfo _);
            _blinds.Add(id, new Blind(loggerFactory.CreateLogger<Blind>(), id, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60), openVariableName, closeVariableName, _adsClient));
        }
    }

    public Task OpenAsync(BlindId id, CancellationToken token)
    {
        if(_blinds.TryGetValue(id, out Blind? blind))
        {
            return blind.OpenAsync(token);
        }
        return Task.CompletedTask;
    }

    public Task CloseAsync(BlindId id, CancellationToken token)
    {
        if (_blinds.TryGetValue(id, out Blind? blind))
        {
            return blind.CloseAsync(token);
        }
        return Task.CompletedTask;
    }

    public Task StopAsync(BlindId id, CancellationToken token)
    {
        if (_blinds.TryGetValue(id, out Blind? blind))
        {
            return blind.StopAsync(token);
        }
        return Task.CompletedTask;
    }
}
