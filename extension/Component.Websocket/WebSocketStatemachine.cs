using System.Net;
using System.Net.WebSockets;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace Component.Websocket;

public class WebSocketStateMachine : IDisposable
{

	public enum OperationalState
	{
		Empty, //- _websocket is not created
		Abort, //- _websocket is exist but with no connection (can be aborted)
		Idle, Text, Binary //- Working States
	}
	public readonly record struct OutboundMessage(ReadOnlyMemory<byte> _messageBytes, WebSocketMessageType _messageType, bool _endOfMessage)
	{
		public void Deconstruct(out ReadOnlyMemory<byte> MessageBytes, out WebSocketMessageType MessageType, out bool EndOfMessage)
		{
			MessageBytes = _messageBytes;
			MessageType = _messageType;
			EndOfMessage = _endOfMessage;
		}
	}

	public readonly record struct InboundMessage(ReadOnlyMemory<byte> _bytes, WebSocketMessageType _messageType, bool _endOfMessage)
	{
		public void Deconstruct(out ReadOnlyMemory<byte> Bytes, out WebSocketMessageType MessageType, out bool EndOfMessage)
		{
			Bytes = _bytes;
			MessageType = _messageType;
			EndOfMessage = _endOfMessage;
		}
	}

	private Task? _mainLoop { get; set; }
	private WebSocket? _webSocket;
	private readonly ILogger Logger;
	private readonly IWebsocketWorker _websocketWorker;
	private readonly CancellationTokenSource _cts;
	private readonly Channel<OutboundMessage> _outBoundChannel = Channel.CreateUnbounded<OutboundMessage>();
	private bool _processing = false;

	public WebSocketStateMachine(WebsocketWorker websocketWorker, ILogger logger)
	{
		_websocketWorker = websocketWorker;
		Logger = logger;
		_cts = new();
	}
	public WebSocketStateMachine(WebsocketWorker websocketWorker, ILogger logger, CancellationToken ct)
	{
		_websocketWorker = websocketWorker;
		Logger = logger;
		_cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
	}

	public OperationalState State
	{
		get
		{
			if (_webSocket is null) return OperationalState.Empty;
			return (_webSocket.State) switch
			{
				WebSocketState.Open => _processing ? OperationalState.Text : OperationalState.Idle,
				_ => OperationalState.Abort
			};
		}
	}

	public Task StartAsync(WebSocket webSocket)
	{
		if (_mainLoop != null)
		{
			throw new InvalidOperationException("The processing loop is still running.");
		}
		_webSocket = webSocket;
		_mainLoop = Task.WhenAny(
			StartContinuousSendLoopAsync(_cts.Token),
			StartContinuousReceiveLoopAsync(_cts.Token)
		);

		return _mainLoop;
	}
	public async ValueTask CloseAcknowledgedAsync()
	{
		if (_webSocket is null) throw new Exception("WebSocket not initialized");
		await _webSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Acknowledged", _cts.Token);
	}
	public async ValueTask CloseIntentionalAsync()
	{
		if (_webSocket is null) throw new Exception("WebSocket not initialized");
		await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Intentional", _cts.Token);
	}
	public ValueTask SendMessageAsync(ReadOnlyMemory<byte> messageBytes, WebSocketMessageType messageType, bool endOfMessage)
		=> _outBoundChannel.Writer.WriteAsync
		(
			new(messageBytes, messageType, endOfMessage),
			_cts.Token
		);
	public bool TrySendMessage(ReadOnlyMemory<byte> messageBytes, WebSocketMessageType messageType, bool endOfMessage)
		=> _outBoundChannel.Writer.TryWrite
		(
			new(messageBytes, messageType, endOfMessage)
		);

	/// <summary>
	/// Continuous SENDER Loop (Outbound)
	/// </summary>
	private async Task StartContinuousSendLoopAsync(CancellationToken ct)
	{
		try
		{
			// Continuously read as long as items are available or the channel isn't completed
			while (
				_webSocket?.State == WebSocketState.Open &&
				await _outBoundChannel.Reader.WaitToReadAsync(ct)
			)
			{
				while (_outBoundChannel.Reader.TryRead(out var message))
				{
					var (messageBytes, messageType, endOfMessage) = message;
					await _webSocket.SendAsync(messageBytes, messageType, endOfMessage, ct);
				}
			}
		}
		catch (OperationCanceledException)
		{
			// Normal execution pathway during shutdown/disposal
			Logger.LogDebug("-- [Send loop] OperationCanceled.");
		}
		catch (WebSocketException ex)
		{
			Logger.LogError(ex, "-- [Send loop] WebSocketException :");
		}
		catch (Exception ex)
		{
			// Log connection exceptions here (e.g., unexpected client drop)
			Logger.LogError(ex, "-- [Send loop] Exception :");
		}
	}

	/// <summary>
	/// Continuous RECEIVER Loop (Inbound)
	/// </summary>
	private async Task StartContinuousReceiveLoopAsync(CancellationToken ct)
	{
		try
		{
			// 64KB buffer for reading chunks.
			var buffer = (new byte[64 * 1024]).AsMemory<byte>();

			while (_webSocket?.State == WebSocketState.Open)
			{
				_processing = false;
				var result = await _webSocket.ReceiveAsync(buffer, ct);
				_processing = true;

				if (result.MessageType is WebSocketMessageType.Close)
				{
					await CloseAcknowledgedAsync();
					break;
				}
				var slice = buffer[..result.Count];
				_websocketWorker.DoAssemble(new(slice, result.MessageType, result.EndOfMessage));
				buffer.Span.Clear(); //- Clear buffer
			}
		}
		catch (OperationCanceledException) { }
		catch (WebSocketException ex) when (ex.InnerException is HttpListenerException)
		{
			Logger.LogDebug(ex, "-- [Receive loop] WebSocketException : Client Force Disconnected Websocket.");
		}
		catch (WebSocketException ex)
		{
			Logger.LogWarning(ex, "-- [Receive loop] WebSocketException :");
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "-- [Receive loop] Exception :");
		}
		finally
		{
			Logger.LogInformation("Dropping Websocket Connection.");
			_websocketWorker.Dispose();
			_outBoundChannel.Writer.TryComplete();
			Logger.LogInformation("Websocket Connection Dropped Successfully.");
		}
	}

	public void Dispose()
	{
		if (!_cts.IsCancellationRequested)
		{
			// 1. Cancel any active ReceiveAsync blocking calls
			_cts.Cancel();
			_cts.Dispose();

			// 2. Safely dispose the native WebSocket resources
			_webSocket?.Dispose();
			_webSocket = null;
			_mainLoop = null;
		}

		GC.SuppressFinalize(this); // Tell GC this is manual GC
	}
}
