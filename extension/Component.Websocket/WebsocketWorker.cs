using System.Net.WebSockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Component.Websocket;

public interface IWebsocketWorker : IDisposable
{
	int BufferSize { get; init; }
	CancellationToken CancellationToken { get; }
	internal void DoAssemble(in InboundMessage inboundMessage);
	void PostReceived(in Stream assembledStream, WebSocketMessageType messageType);
}

public abstract class WebsocketWorker : IWebsocketWorker
{
	protected virtual ILogger<IWebsocketWorker> Logger { get; init; } = default!;
	public int BufferSize { get; init; } = 64 * 1024;
	public CancellationToken CancellationToken { get => WebSocketStateMachine?.InternalCts.Token ?? default; }
	public virtual WebSocketStateMachine? WebSocketStateMachine { get; protected set; }

	public virtual bool HasConnection => (WebSocketStateMachine?.State) switch
	{
		OperationalState.Idle => true,
		OperationalState.Working => true,
		_ => false
	};

	protected Stream? _assembleStream;
	private readonly Lock _assembleLock = new();
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
	public virtual async Task StartAsync(WebSocket webSocket, params CancellationToken[] cts)
	{
		if (WebSocketStateMachine is not null) throw new InvalidOperationException("Websocket Connection is already Established...");

		WebSocketStateMachine = new(this, Logger, cts);
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
	public ValueTask SendAsync(ReadOnlyMemory<byte> payload, WebSocketMessageType messageType, bool endOfMessage)
	{
		ArgumentNullException.ThrowIfNull(WebSocketStateMachine, nameof(WebSocketStateMachine));
		return WebSocketStateMachine.SendMessageAsync(payload, messageType, endOfMessage);
	}
	public bool TrySend(ReadOnlyMemory<byte> payload, WebSocketMessageType messageType, bool endOfMessage)
	{
		ArgumentNullException.ThrowIfNull(WebSocketStateMachine, nameof(WebSocketStateMachine));
		return WebSocketStateMachine.TrySendMessage(payload, messageType, endOfMessage);
	}

	protected virtual bool TryAssemble(in InboundMessage inboundMessage)
	{
		var (bytes, _, endOfMessage) = inboundMessage;

		lock (_assembleLock)
		{
			this.AssembleStream?.Write(bytes.Span);
		}
		return endOfMessage;
	}
	internal virtual void DoAssemble(in InboundMessage inboundMessage)
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
			OperationalState.Working => new MemoryStream(),
			_ => throw new NotImplementedException($"State \"{this.WebSocketStateMachine?.State}\" is not supported for assembly.")
		};
	}

	public abstract void PostReceived(in Stream assembledStream, WebSocketMessageType messageType);
	protected virtual void PostDisconnect()
	{
		AssembleStream = null;
		WebSocketStateMachine = null;
	}

	void IWebsocketWorker.DoAssemble(in InboundMessage inboundMessage) => DoAssemble(inboundMessage);
	public virtual void Dispose()
	{
		AssembleStream = null;
		WebSocketStateMachine?.Dispose();
		WebSocketStateMachine = null;

		GC.SuppressFinalize(this);
	}
}
