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

#pragma warning disable CDT1004
public sealed class ServiceInteractions
{
	private const string Secret = "secret.json";
	private readonly ILogger<ServiceInteractions> Logger;
	public readonly WebsocketClient WsClient;
	private readonly Arma3ServiceSecret ServiceSecret;

	internal string AccessName { get; private set; } = "";

	public event Action<ProfileConfiguration, IdentityRolesReturnPayload> ServiceAccessResult = (configuration, authTokenPayload) =>
	{
		var returnPayloadString = JsonSerializer.Serialize(
			authTokenPayload,
			IdentityRolesPayloadJsonSerializerContext.Default.IdentityRolesReturnPayload
		);

		Arma3PayloadCallBack serviceAccessResult = new(
			Function: "ServiceAccessResult",
			Data: $"[{authTokenPayload is not { AuthToken: null }},{returnPayloadString}]"
		);
		Util.CallExtensionCallback(Callback, serviceAccessResult);

		var configString = JsonSerializer.Serialize(
			configuration,
			ProfileConfigurationJsonSerializerContext.Default.ProfileConfiguration
		);

		Arma3PayloadCallBack profileUpdated = new(
			Function: "ProfileUpdated",
			Data: configString
		);
		Util.CallExtensionCallback(Callback, profileUpdated);
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
			Logger.LogWarning("WebSocket connection already established.");
			return;
		}

		var (profileConfig, tokenPayload) = await GetAccessToken(accessName, profilePayload);
		await WsClient.StartAsync(ServiceSecret.WebSocketServiceUri, tokenPayload.AuthToken);

		//- Send Profile Configs
		if (tokenPayload.IsDifferent || tokenPayload.IsDifferent)
		{
			await SendWebSocketUpdateAndSaveProfile(profileConfig.Configuration);
		}
	}
	public Task DisconnectWebSocket()
	{
		if (!WsClient.HasConnection)
		{
			Logger.LogWarning("WebSocket connection not established, skipping disconnection.");
			return Task.CompletedTask;
		}
		return WsClient.CloseAsync();
	}
	public async Task ReconnectWebSocket(string profilePayload)
	{
		await DisconnectWebSocket();
		await EstablishWebSocketConnection(AccessName, profilePayload);
	}
	public ValueTask SendWebSocketMessage(string messageJson)
		=> SendWebSocketMessageAsync(messageJson);

	internal ValueTask SendWebSocketMessageAsync(string messageJson)
		=> WsClient.SendAsync(messageJson, WebSocketMessageType.Text, true);

	public ValueTask SendWebSocketMessage(ReadOnlyMemory<byte> message)
		=> SendWebSocketMessageAsync(message);

	internal ValueTask SendWebSocketMessageAsync(ReadOnlyMemory<byte> message)
		=> WsClient.SendAsync(message, WebSocketMessageType.Text, true);

	public async Task SendWebSocketBinaries(Dictionary<string, string> binaryDict)
	{
		Logger.LogInformation("Start Sending binaries.");
		foreach (var (directoryPrefix, filePath) in binaryDict)
		{
			Logger.LogInformation("Binary : [Prefix - {Prefix}, {Path}]", directoryPrefix, filePath);
			await SendWebSocketBinary(filePath, directoryPrefix);
		}
		Logger.LogInformation("End of Sending binaries.");
	}
	private async Task SendWebSocketUpdateAndSaveProfile(Arma3ClientProfileConfiguration configuration)
	{
		Logger.LogInformation("Sending profileConfig.");

		var fileList = configuration.GetTemplateFileList(Util.AssemblyPath);
		var payloadBinaries = configuration.ToPayloadBinaryList(Util.AssemblyPath);
		Arma3PayloadUpdateDB payloadUpdateDB = new(
			new UpdateAndSaveProfile(payloadBinaries, configuration)
		);

		var configByte = payloadUpdateDB.ToJsonBytes();
		await WsClient.SendAsync(configByte, WebSocketMessageType.Text, true);

		foreach (var (payloadBinary, index) in payloadBinaries.Select((v, i) => (v, i)))
		{
			var filePath = fileList[index];
			await WsClient.SendBinaryAsync(AccessName, filePath, payloadBinary);
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
	public async Task SendWebSocketBinary(string filePath, string directoryPrefix)
	{
		FileInfo fileInfo = new(filePath);
		Logger.LogInformation("Sending binary file \"{FileName}\"", fileInfo.Name);

		// Send Metadata (as text message)
		Arma3PayloadBinary metadata = new
		(
			fileInfo.Name,
			fileInfo.Length,
			fileInfo.CreationTime,
			directoryPrefix
		);

		var bytes = metadata.ToJsonBytes();
		await WsClient.SendAsync(bytes, WebSocketMessageType.Binary, true);
		await WsClient.SendBinaryAsync(AccessName, filePath, metadata);
	}

	/// <summary>
	/// This method securely authenticates with a backend service using credentials from a configuration file to obtain a temporary access token for making further API calls.
	/// </summary>
	private async Task<(ProfileConfiguration, IdentityRolesReturnPayload)> GetAccessToken(string accessName, string profileName)
	{
		if (string.IsNullOrEmpty(AccessName) || accessName != AccessName)
			AccessName = accessName;

		var profileConfig = GetServiceProfile(profileName);

		//- Send Request for access token
		var payload = new IdentityRolesPayload
		{
			Identity = new IdentityInfo
			{
				AccessName = AccessName,
				Role = Role.GameServer
			},
			ExpireMinute = 15,
			ProfileDateOffsets = profileConfig.GetDateOffsets()
		};
		var jsonPayload = JsonSerializer.SerializeToUtf8Bytes(
			payload,
			IdentityRolesPayloadJsonSerializerContext.Default.IdentityRolesPayload
		);

		const int MaxRetry = 5;
		for (int i = 1; i < MaxRetry + 1; i++)
		{
			var retrySeconds = Math.Pow(i, 2);
			var nextRetrySeconds = Math.Pow(i + 1, 2);

			var waitSpan = TimeSpan.FromSeconds(retrySeconds);
			await Task.Delay(waitSpan);

			byte[]? ResponseContentBytes = null;
			try
			{
				using ReadOnlyMemoryContent content = new(jsonPayload);
				content.Headers.ContentType = new(MediaTypeNames.Application.Json);
				using var response = await APIRequest.PostRequest(
					ServiceSecret.ServiceUri + "/api/token",
					content: content,
					authHeader: new AuthenticationHeaderValue(
						"Bearer",
						ServiceSecret.Secret.ApiKey
					)
				);

				if (!response.IsSuccessStatusCode)
				{
					Logger.LogWarning(
						"({Retry}/{MaxRetry}) API request failed with status code: {StatusCode}({ReasonPhrase}). Retrying after {RetrySeconds} Seconds...",
						i,
						MaxRetry,
						response.StatusCode,
						response.ReasonPhrase,
						nextRetrySeconds
					);
					continue;
				}

				//- Get the response content
				ResponseContentBytes = await response.Content.ReadAsByteArrayAsync();
				ArgumentNullException.ThrowIfNull(ResponseContentBytes);
			}
			catch (ArgumentNullException)
			{
				Logger.LogError(
					"({Retry}/{MaxRetry}) API empty response Content. Retrying after {RetrySeconds} Seconds...",
					i,
					MaxRetry,
					nextRetrySeconds
				);
				continue;
			}
			catch (HttpRequestException ex)
			{
				Logger.LogError(
					"({Retry}/{MaxRetry}) API request failed due to \"{Reason}\".\n Retrying after {RetrySeconds} Seconds...",
					i,
					MaxRetry,
					ex.Message,
					nextRetrySeconds
				);
				continue;
			}

			//- Get the Token
			var authTokenPayload = JsonSerializer.Deserialize(
				ResponseContentBytes,
				IdentityRolesPayloadJsonSerializerContext.Default.IdentityRolesReturnPayload
			);
			Logger.LogTrace("Token Manager (result) : {TokenPayload}", authTokenPayload);
			Logger.LogTrace("profileConfig (result) : {profileConfig}", profileConfig);

			//- Established Socket Connection
			ServiceAccessResult.Invoke(profileConfig, authTokenPayload);

			return (profileConfig, authTokenPayload);
		}

		throw new InvalidOperationException("All retry failed.");
	}
	public ProfileConfiguration GetServiceProfile(string profileName)
	{
		var fileName = Path.Combine("profiles", profileName + ".json");
		var profileString = Util.ParseJson(fileName)
			?? throw new FileNotFoundException($"Profile file '{Path.Combine("profile", profileName + ".json")}' not found or could not be parsed.");

		Logger.LogTrace("UpdateServiceProfile : {Profile}", profileString);

		var profileConfiguration = JsonSerializer.Deserialize(
			profileString,
			ProfileConfigurationJsonSerializerContext.Default.ProfileConfiguration
		);

		//- Add Assembly Prefix
		string[] clientProfileConfig = profileConfiguration.Configuration.GetTemplateFileList(Util.AssemblyPath);
		Logger.LogTrace("clientProfileConfig : Length - {Length}", clientProfileConfig.Length);

		profileConfiguration = profileConfiguration with
		{
			Configuration = new Arma3ClientProfileConfiguration(
				clientProfileConfig[0],
				clientProfileConfig[1],
				clientProfileConfig.Length < 2 ?
					null :
					clientProfileConfig[2]
			)
		};
		Logger.LogTrace("UpdateServiceProfile : {ProfileConfig}", profileConfiguration);

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
}
