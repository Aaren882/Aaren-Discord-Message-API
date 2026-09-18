using System.Net.Http.Headers;
using System.Net.Mime;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Components.Entity;
using DiscordMessageAPI.ServiceConnection.WebService;
using ExtensionComponents.Tools;
using Microsoft.Extensions.Logging;
using static ExtensionComponents.ExtensionStartup;

namespace ServiceConnection.WebService;

public sealed class ServiceInteractions
{
	private const string Secret = "secret.json";
	private readonly ILogger<ServiceInteractions> Logger;
	public readonly WebsocketClient WsClient;
	private readonly Arma3ServiceSecret ServiceSecret;
	internal ProfileConfiguration ProfileConfig;

	internal string AccessName { get; private set; } = "";

	public event Action<ProfileConfiguration, IdentityRolesReturnPayload>? ServiceAccessResult = (configuration, authTokenPayload) =>
	{
		Arma3PayloadCallBack callBack = new(
			Function: "ServiceAccessResult",
			Data: $"[{authTokenPayload is not { AuthToken: null }},{configuration}]"
		);
		Util.CallExtensionCallback(Callback, callBack);
	};

	private string? _RPTFileDirectory { get; set; }
	public string RPTFileDirectory
	{
		get => _RPTFileDirectory ?? throw new DirectoryNotFoundException("It seems \"RPTDirectory\" didn't get initiate correctly.");
		internal set => _RPTFileDirectory = value;
	}

	public ServiceInteractions(ILogger<ServiceInteractions> logger, WebsocketClient websocket)
	{
		Logger = logger;
		ServiceSecret = GetServiceSecret();
		if (ServiceSecret.RPT_Directory != null)
			RPTFileDirectory = Path.GetFullPath(ServiceSecret.RPT_Directory);

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

		//- Send Profile Configs
		if (tokenPayload.IsDifferent || tokenPayload.IsDifferent)
		{
			await SendWebSocketUpdateAndSaveProfile(ProfileConfig.Configuration);
		}
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
	public ValueTask SendWebSocketMessage(string messageJson)
		=> SendWebSocketMessageAsync(messageJson);

	internal ValueTask SendWebSocketMessageAsync(string messageJson)
		=> WsClient.SendAsync(messageJson, WebSocketMessageType.Text, true);

	public async Task SendWebSocketBinaries(Dictionary<string, string> binaryDict, int chunkSize = 64 * 1024)
	{
		Logger.LogInformation("Start Sending binaries.");
		foreach (var (directoryPrefix, filePath) in binaryDict)
		{
			Logger.LogInformation("Binary : [Prefix - {Prefix}, {Path}]", directoryPrefix, filePath);
			await SendWebSocketBinary(filePath, directoryPrefix, chunkSize);
		}
		Logger.LogInformation("End of Sending binaries.");
	}
	private async Task SendWebSocketUpdateAndSaveProfile(Arma3ClientProfileConfiguration configuration, int chunkSize = 64 * 1024)
	{
		Logger.LogInformation("Sending profileConfig.");

		var fileList = configuration.GetTemplateFileList();
		var payloadBinaries = configuration.ToPayloadBinaryList();
		Arma3PayloadUpdateDB payloadUpdateDB = new(
			new UpdateAndSaveProfile(payloadBinaries, configuration)
		);

		var configStr = JsonSerializer.Serialize(payloadUpdateDB, Arma3PayloadJsonSerializerContext.Default.Arma3Payload);
		await WsClient.SendAsync(configStr, WebSocketMessageType.Text, true);

		foreach (var (payloadBinary, index) in payloadBinaries.Select((v, i) => (v, i)))
		{
			var filePath = fileList[index];
			// var bytes = JsonSerializer.SerializeToUtf8Bytes(payloadBinary, Arma3PayloadJsonSerializerContext.Default.Arma3Payload);
			// await WsClient.SendAsync(bytes, WebSocketMessageType.Binary, true);
			await WsClient.SendBinaryAsync(AccessName, filePath, payloadBinary, chunkSize);
		}
		Logger.LogInformation("profileConfig Sent.");
	}

	public void SendWebSocketRptLines(string filePath, int linesCount)
	{
		Logger.LogInformation("Sending RPT \"{LinesNum}\" lines", linesCount);
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
	public async Task SendWebSocketBinary(string filePath, string directoryPrefix, int chunkSize = 64 * 1024)
	{
		FileInfo fileInfo = new(filePath);
		var totalChunks = (int)Math.Ceiling((double)fileInfo.Length / chunkSize);
		Logger.LogInformation("Sending binary file \"{FileName}\"", fileInfo.Name);

		// Send Metadata (as text message)
		Arma3PayloadBinary metadata = new
		(
			fileInfo.Name,
			fileInfo.Length,
			fileInfo.CreationTime,
			totalChunks,
			directoryPrefix
		);

		// Task.Run(async () =>
		// {
		var bytes = JsonSerializer.SerializeToUtf8Bytes(metadata, Arma3PayloadJsonSerializerContext.Default.Arma3Payload);
		await WsClient.SendAsync(bytes, WebSocketMessageType.Binary, true);
		await WsClient.SendBinaryAsync(AccessName, filePath, metadata, chunkSize);
		// });

		/* SocketLocalWorker.WebSocketTrafficWriter(
			metadata,
			() => WsClient.SendBinaryAsync(filePath, metadata, chunkSize)
		); */
	}

	/// <summary>
	/// This method securely authenticates with a backend service using credentials from a configuration file to obtain a temporary access token for making further API calls.
	/// </summary>
	private async Task<IdentityRolesReturnPayload> GetAccessToken(string accessName, string profileName)
	{
		try
		{
			if (string.IsNullOrEmpty(AccessName) || accessName != AccessName)
				AccessName = accessName;

			ProfileConfig = GetServiceProfile(profileName);

			//- Send Request for access token
			var payload = new IdentityRolesPayload
			{
				Identity = new IdentityInfo
				{
					AccessName = AccessName,
					Role = Role.GameServer
				},
				ExpireMinute = 15,
				ProfileDateOffsets = ProfileConfig.GetDateOffsets()
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
			Logger.LogTrace("Token Manager (result) : {TokenPayload}", authTokenPayload);

			//- Established Socket Connection
			ServiceAccessResult?.Invoke(ProfileConfig, authTokenPayload);

			return authTokenPayload;
		}
		catch (Exception e)
		{
			Logger.LogError(e, "An error occurred during service interaction: {Message}", e.Message);
			throw;
		}
	}
	public ProfileConfiguration GetServiceProfile(string profileName)
	{
		var fileName = Path.Combine("profiles", profileName + ".json");
		var profileString = Util.ParseJson(fileName)
			?? throw new FileNotFoundException($"Profile file '{Path.Combine("profile", profileName + ".json")}' not found or could not be parsed.");

		var profileConfiguration = JsonSerializer.Deserialize(
			profileString,
			ProfileConfigurationJsonSerializerContext.Default.ProfileConfiguration
		);

		Logger.LogTrace("UpdateServiceProfile : {Profile}", profileString);
		return profileConfiguration;
	}
	private Arma3ServiceSecret GetServiceSecret()
	{
		var secretString = Util.ParseJson(Secret);
		var tokenPayload = JsonSerializer.Deserialize(
			secretString,
			Arma3PayloadJsonSerializerContext.Default.Arma3ServiceSecret
		)!;

		Logger.LogTrace("GetServiceSecret : {Secret}", secretString);
		return tokenPayload;
	}
	private static string GetBasicAuthenticationBearer(Arma3ServiceSecret serviceSecret)
	{
		return serviceSecret.Secret.ToString();
	}
}
