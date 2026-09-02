using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Components.Entity;
using DiscordMessageAPI.ServiceConnection.WebService;
using ExtensionComponents.Tools;
using static ExtensionComponents.ExtensionStartup;

namespace ServiceConnection.WebService;

public sealed class ServiceInteractions
{
	private const string Secret = "secret.json";
	private readonly Arma3ServiceSecret ServiceSecret;

	internal string AccessName { get; private set; } = "";

	public event Action<IdentityRolesReturnPayload>? ServiceAccessResult = (authTokenPayload) =>
	{
		Arma3PayloadCallBack callBack = new(
			Function: "ServiceAccessResult",
			Data: $"[{authTokenPayload is not { AuthToken: null }},{authTokenPayload.AdditionalPayload ?? "[]"}]"
		);
		Util.CallExtensionCallback(Callback, callBack);
	};
	public WebsocketClient WsClient { get; init; }
	// public readonly WebSocketLocalWorker SocketLocalWorker = new();

	public string? RPTDirectory { get; internal set; }

	public ServiceInteractions(WebsocketClient websocket)
	{
		ServiceSecret = GetServiceSecret();
		if (ServiceSecret.RPT_Directory != null)
			RPTDirectory = Path.GetFullPath(ServiceSecret.RPT_Directory);

		WsClient = websocket;
		WsClient.Connected += () =>
		{
			Arma3PayloadCallBack callBack = new(
				Function: "ConnectionChanged",
				Data: "[true]"
			);
			Util.CallExtensionCallback(Callback, callBack);
		};
		WsClient.Disconnected += () =>
		{
			Arma3PayloadCallBack callBack = new(
				Function: "ConnectionChanged",
				Data: "[false]"
			);
			Util.CallExtensionCallback(Callback, callBack);
		};
		WsClient.MessageReceived += (message) =>
		{
			// Tracer("MessageReceived (message)", message.ToString());
			Util.CallExtensionCallback(Callback, message);
		};
	}

	public async Task EstablishWebSocketConnection(string accessName, string profilePayload)
	{
		if (WsClient.HasConnection)
		{
			Logger(null, "WebSocket connection already established.");
			return;
		}

		var tokenPayload = await GetAccessToken(accessName, profilePayload);
		await WsClient.StartAsync(ServiceSecret.WebSocketServiceUri, tokenPayload.AuthToken);
	}
	public Task DisconnectWebSocket(string description = "Client disconnect")
	{
		return WsClient.CloseAsync();
	}
	public async Task ReconnectWebSocket(string profilePayload)
	{
		await DisconnectWebSocket("Client Reconnecting");
		await EstablishWebSocketConnection(AccessName, profilePayload);
	}
	public void SendWebSocketMessage(string messageJson)
		=> Task.Run(async () => await SendWebSocketMessageAsync(messageJson));

	internal ValueTask SendWebSocketMessageAsync(string messageJson)
		=> WsClient.SendAsync(messageJson, WebSocketMessageType.Text, true);

	public void SendWebSocketBinaries(Dictionary<string, string> binaryDict, int chunkSize = 64 * 1024)
	{
		Logger(null, "INFO: Sending binaries");
		foreach (var (directoryPrefix, filePath) in binaryDict)
			SendWebSocketBinary(filePath, directoryPrefix, chunkSize);
	}

	public void SendWebSocketRptLines(string filePath, int linesCount)
	{
		Logger(null, $"INFO: Sending RPT \"{linesCount}\" lines");
		var fileInfo = new FileInfo(filePath);
		var metadata = new Arma3PayloadRptLine
		(
			fileInfo.Name,
			fileInfo.CreationTime
		);

		/* SocketLocalWorker.WebSocketTrafficWriter(
			metadata,
			() => WsClient.SendRptLinesAsync(filePath, linesCount)
		); */
	}
	public void SendWebSocketBinary(string filePath, string directoryPrefix, int chunkSize = 64 * 1024)
	{
		FileInfo fileInfo = new(filePath);
		var totalChunks = (int)Math.Ceiling((double)fileInfo.Length / chunkSize);
		Logger(null, $"INFO: Sending binary file \"{fileInfo.Name}\"");

		// Send Metadata (as text message)
		Arma3PayloadBinary metadata = new
		(
			fileInfo.Name,
			fileInfo.Length,
			fileInfo.CreationTime,
			totalChunks,
			directoryPrefix
		);

		Task.Run(async () =>
		{
			var bytes = JsonSerializer.SerializeToUtf8Bytes(metadata, Arma3PayloadJsonSerializerContext.Default.Arma3Payload);
			await WsClient.SendAsync(bytes, WebSocketMessageType.Binary, true);
			await WsClient.SendBinaryAsync(AccessName, filePath, metadata, chunkSize);
		});

		/* SocketLocalWorker.WebSocketTrafficWriter(
			metadata,
			() => WsClient.SendBinaryAsync(filePath, metadata, chunkSize)
		); */
	}

	/// <summary>
	/// This method securely authenticates with a backend service using credentials from a configuration file to obtain a temporary access token for making further API calls.
	/// </summary>
	private async Task<IdentityRolesReturnPayload> GetAccessToken(string accessName, string profilePayload)
	{
		try
		{
			if (string.IsNullOrEmpty(AccessName) || accessName != AccessName)
				AccessName = accessName;

			if (profilePayload is null)
			{
				throw new Exception("INFO: No profile found.");
			}

			//- Send Request for access token
			var payload = new IdentityRolesPayload
			{
				Identity = new IdentityInfo
				{
					AccessName = AccessName,
					Role = Role.GameServer
				},
				ExpireMinute = 15,
				AdditionalPayload = profilePayload
			};
			var jsonPayload = JsonSerializer.Serialize(
				payload,
				IdentityRolesPayloadJsonSerializerContext.Default.IdentityRolesPayload
			);

			using var response = await APIRequest.PostRequest(
				ServiceSecret.ServiceUri + "/api/token",
				content: new StringContent(
					jsonPayload,
					Encoding.UTF8, MediaTypeNames.Application.Json
				),
				authHeader: new AuthenticationHeaderValue(
					"Basic",
					GetBasicAuthenticationBearer(ServiceSecret)
				)
			);

			//- Get the Token
			var result = await response.Content.ReadAsStringAsync();
			var authTokenPayload = JsonSerializer.Deserialize(
				result,
				IdentityRolesPayloadJsonSerializerContext.Default.IdentityRolesReturnPayload
			)!;
			Tracer("Token Manager (result)", authTokenPayload.ToString());

			/*if (authTokenPayload is { AuthToken: null })
				throw new NullReferenceException($"{nameof(authTokenPayload)} is null.");*/

			//- Established Socket Connection
			ServiceAccessResult?.Invoke(authTokenPayload);

			return authTokenPayload;
		}
		catch (Exception e)
		{
			Logger(e, "");
			throw;
		}
	}
	private static Arma3ServiceSecret GetServiceSecret()
	{
		var secretString = Util.ParseJson(Secret);
		var tokenPayload = JsonSerializer.Deserialize(
			secretString,
			Arma3PayloadJsonSerializerContext.Default.Arma3ServiceSecret
		)!;

		Tracer("GetServiceSecret", secretString);
		return tokenPayload;
	}
	private static string GetBasicAuthenticationBearer(Arma3ServiceSecret serviceSecret)
	{
		return serviceSecret.Secret.ToString();
	}
}
