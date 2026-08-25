using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Component.Websocket;

public interface IWebsocketWorker : IDisposable
{
    internal void DoAssemble(in WebSocketStateMachine.InboundMessage inboundMessage);
    void PostReceived(in Stream assembledStream, WebSocketMessageType messageType);
}

public abstract class WebsocketWorker : IWebsocketWorker
{
    protected virtual ILogger<IWebsocketWorker> Logger { get; init; } = default!;
    public virtual WebSocketStateMachine? WebSocketStateMachine { get; protected set; }
    public CancellationTokenSource CancellationTokenSource = new();

    public virtual bool HasConnection => (WebSocketStateMachine?.State) switch
    {
        WebSocketStateMachine.OperationalState.Idle => true,
        WebSocketStateMachine.OperationalState.Text => true,
        WebSocketStateMachine.OperationalState.Binary => true,
        _ => false
    };

    protected Stream? _assembleStream;
    private readonly object _assembleLock = new();
    protected virtual Stream? AssembleStream
    {
        get
        {
            _assembleStream ??= GetAssembleStream();
            return _assembleStream;
        }
        set
        {
            _assembleStream?.Dispose();
            if (value is null) _assembleStream = null;
        }
    }

    public virtual async Task StartAsync(WebSocket webSocket)
    {
        if (WebSocketStateMachine is not null) throw new InvalidOperationException("Websocket Connection is already Established...");

        WebSocketStateMachine = new(this, Logger);
        await WebSocketStateMachine.StartAsync(webSocket);
    }
    public virtual async Task StartAsync(WebSocket webSocket, CancellationToken ct)
    {
        if (WebSocketStateMachine is not null) throw new InvalidOperationException("Websocket Connection is already Established...");

        WebSocketStateMachine = new(this, Logger, ct);
        await WebSocketStateMachine.StartAsync(webSocket);
    }
    public virtual async Task CloseAsync()
    {
        if (WebSocketStateMachine is null) throw new InvalidOperationException("No websocket Connection is Established...");

        await WebSocketStateMachine.CloseIntentionalAsync();
    }

    public ValueTask SendAsync(string payload, WebSocketMessageType messageType, bool endOfMessage)
    {
        ArgumentNullException.ThrowIfNull(WebSocketStateMachine, nameof(WebSocketStateMachine));
        return SendAsync(Encoding.UTF8.GetBytes(payload), messageType, endOfMessage);
    }
    public bool TrySend(string payload, WebSocketMessageType messageType, bool endOfMessage)
    {
        ArgumentNullException.ThrowIfNull(WebSocketStateMachine, nameof(WebSocketStateMachine));
        return TrySend(Encoding.UTF8.GetBytes(payload), messageType, endOfMessage);
    }
    public ValueTask SendAsync(byte[] payload, WebSocketMessageType messageType, bool endOfMessage)
    {
        ArgumentNullException.ThrowIfNull(WebSocketStateMachine, nameof(WebSocketStateMachine));
        return WebSocketStateMachine.SendMessageAsync(payload, messageType, endOfMessage);
    }
    public bool TrySend(byte[] payload, WebSocketMessageType messageType, bool endOfMessage)
    {
        ArgumentNullException.ThrowIfNull(WebSocketStateMachine, nameof(WebSocketStateMachine));
        return WebSocketStateMachine.TrySendMessage(payload, messageType, endOfMessage);
    }

    protected virtual bool TryAssemble(in WebSocketStateMachine.InboundMessage inboundMessage)
    {
        var (bytes, _, endOfMessage) = inboundMessage;

        lock (_assembleLock)
        {
            this.AssembleStream?.Write(bytes.Span);
        }
        return endOfMessage;
    }
    internal virtual void DoAssemble(in WebSocketStateMachine.InboundMessage inboundMessage)
    {
        if (!TryAssemble(inboundMessage)) return;
        var (_, messageType, _) = inboundMessage;

        AssembleStream!.Position = 0;
        PostReceived(this.AssembleStream!, messageType);

        this.AssembleStream = null; //- Clean up written Stream
    }
    protected virtual Stream GetAssembleStream()
    {
        return this.WebSocketStateMachine?.State switch
        {
            WebSocketStateMachine.OperationalState.Text => new MemoryStream(),
            _ => throw new NotImplementedException($"State \"{this.WebSocketStateMachine?.State}\" is not supported for assembly.")
        };
    }

    public abstract void PostReceived(in Stream assembledStream, WebSocketMessageType messageType);
    protected virtual void PostDisconnect()
    {
        AssembleStream = null;
        WebSocketStateMachine = null;
    }

    void IWebsocketWorker.DoAssemble(in WebSocketStateMachine.InboundMessage inboundMessage) => DoAssemble(inboundMessage);
    public virtual void Dispose()
    {
        AssembleStream = null;
        WebSocketStateMachine?.Dispose();
        WebSocketStateMachine = null;

        GC.SuppressFinalize(this);
    }
}
