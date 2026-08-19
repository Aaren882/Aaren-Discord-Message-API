using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Component.Websocket;
using Components.Entity;
using Microsoft.Extensions.Logging;

namespace ServiceConnection.WebService;

public sealed class WebsocketClient(
	ILogger<IWebsocketWorker> logger,
	ServiceRequestHandler serviceRequestHandler
) : WebsocketWorker
{
	protected override ILogger<IWebsocketWorker> Logger => logger;
	public event Action<Arma3Payload>? MessageReceived;
	public event Action? Connected;
	public event Action? Disconnected;

	public override void PostReceived(in Stream assembledStream, WebSocketMessageType messageType)
	{
		using StreamReader reader = new(assembledStream, Encoding.UTF8);
		var receivedMessage = reader.ReadToEnd();
		if (string.IsNullOrEmpty(receivedMessage))
		{
			Logger.LogWarning("Received empty \"{MessageType}\" Message.", messageType);
			return;
		}

		var payload = JsonSerializer.Deserialize(
			receivedMessage,
			Arma3PayloadJsonSerializerContext.Default.Arma3Payload
		)!;
		if (payload is Arma3PayloadServiceRequest request)
		{
			Task.Run(async () => await serviceRequestHandler.RespondRequest(request))
				.GetAwaiter().GetResult();
		}
		MessageReceived?.Invoke(payload);
	}
	public async ValueTask SendBinaryAsync(string accessName, string filePath, Arma3PayloadBinary payloadBinary, int chunkSize = 60 * 1024)
	{
		ArgumentNullException.ThrowIfNull(WebSocketStateMachine, nameof(WebSocketStateMachine));
		Logger.LogInformation("Sending Binary: \n File: {File} \n Header: {header}", filePath, payloadBinary);

		// Send Chunks (as binary messages)
		await using (FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, chunkSize))
		{
			var readBuffer = (new byte[chunkSize]).AsMemory<byte>();
			var identifier = payloadBinary.GetIdentifier(accessName);

			for (var i = 1; i < payloadBinary.TotalChunks + 1; i++)
			{
				int readLength = await fs.ReadAsync(readBuffer, CancellationToken.None);
				Arma3PayloadBinaryContent content = new(identifier, readBuffer.ToArray(), i == payloadBinary.TotalChunks);

				Logger.LogDebug("SendBinaryAsync (Progress): {i}/{TotalChunks}", i, payloadBinary.TotalChunks);
				var payload = JsonSerializer.SerializeToUtf8Bytes(
					content,
					Arma3PayloadJsonSerializerContext.Default.Arma3Payload
				);
				await WebSocketStateMachine.SendMessageAsync(payload, WebSocketMessageType.Binary, true);
			}
		}

		Logger.LogInformation("Sent Binary: {File}", filePath);
	}
	public async Task StartAsync(string uri, string? authToken)
	{
		if (WebSocketStateMachine is not null) throw new InvalidOperationException("Websocket Connection is already Established...");

		var webSocket = new ClientWebSocket();
		if (authToken != null)
			webSocket.Options.SetRequestHeader("Authorization", "Bearer " + authToken);

		await webSocket.ConnectAsync(new(uri), CancellationToken.None);
		Logger.LogInformation("Connected to server.");
		Connected?.Invoke();

		WebSocketStateMachine = new(this, Logger);
		await WebSocketStateMachine.StartAsync(webSocket);
	}
	public override async Task CloseAsync()
	{
		await base.CloseAsync();
		Disconnected?.Invoke();
		Logger.LogInformation("Disconnected from server.");
	}
}
