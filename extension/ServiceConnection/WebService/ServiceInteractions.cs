using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using Components.Entity;
using DiscordMessageAPI.ServiceConnection.WebService;
using ServiceConnection.Tools;
using static ServiceConnection.ServiceStartup;
using System.Text.Json;

namespace ServiceConnection.WebService;

public class ServiceInteractions
{
	private const string Secret = "secret.json"; 
	private static readonly Arma3ServiceSecret ServiceSecret = GetServiceSecret();
	
	private string? AccessName;
	public readonly string RPTDirectory = Path.GetFullPath(GetServiceSecret().RPT_Directory);

	public event Action<IdentityRolesReturnPayload>? ServiceAccessResult = (authTokenPayload) => {
		var callBack = new Arma3PayloadCallBack(
			Data : $"[{authTokenPayload is not { AuthToken: null }},{authTokenPayload.AdditionalPayload ?? "[]"}]",
			Function : "ServiceAccessResult"
		);
		Util.CallExtensionCallback(Callback, callBack);
	};
	public readonly WebSocketClient WsClient = new (ServiceSecret.WebSocketServiceUri + "/api/ws/ingame");

	public ServiceInteractions()
	{
		WsClient.Connected += () =>
		{
			Logger(null, "INFO: Connected to server");
			
			var callBack = new Arma3PayloadCallBack(
				Data : "[true]",
				Function : "ConnectionChanged"
			);
			Util.CallExtensionCallback(Callback, callBack);
		};
		WsClient.Disconnected += () =>
		{
			Logger(null, "INFO: Disconnected from server");
			
			var callBack = new Arma3PayloadCallBack(
				Data : "[false]",
				Function : "ConnectionChanged"
			);
			Util.CallExtensionCallback(Callback, callBack);
		};
		WsClient.MessageReceived += (message) =>
		{
			Tracer("MessageReceived (message)", message.ToString());
			Util.CallExtensionCallback(Callback, message);
		};
	}
	
	public async Task EstablishWebSocketConnection(string accessName, string profilePayload)
	{
		if (WsClient.Status() == WebSocketState.Open)
		{
			Logger(null, "WebSocket connection already established.");
			return;
		}
		
		var tokenPayload = await GetAccessToken(accessName, profilePayload);
		await WsClient.ConnectAsync(tokenPayload.AuthToken);
	}
	public async Task DisconnectWebSocket(string description = "Client disconnect")
	{
		await WsClient.DisconnectAsync(description);
	}
	public async Task ReconnectWebSocket(string profilePayload)
	{
		await DisconnectWebSocket("Client Reconnecting");
		await EstablishWebSocketConnection(AccessName, profilePayload);
	}
	public Task SendWebSocketMessage(string messageJson)
		=> WsClient.SendMessageAsync(messageJson);

	public async Task SendWebSocketBinaries(Dictionary<string,string> binaryDict, int chunkSize = 64 * 1024)
	{
		Logger(null, "INFO: Sending binaries");
		foreach (var path in binaryDict)
			await SendWebSocketBinary(path);
	}

	public async Task SendWebSocketBinary(string filePath, string directoryPrefix, int chunkSize = 64 * 1024)
    {
	    var fileInfo = new FileInfo(filePath);
	    var totalChunks = (int)Math.Ceiling((double)fileInfo.Length / chunkSize);

	    // Send Metadata (as text message)
	    var metadata = new Arma3PayloadBinary
	    (
		    fileInfo.Name,
		    fileInfo.Length,
		    fileInfo.CreationTime,
		    totalChunks,
		    directoryPrefix
	    );
		var metaJson = JsonSerializer.Serialize(metadata, Arma3PayloadJsonSerializerContext.Default.Arma3Payload);

		try
		{
			await SendWebSocketMessage(metaJson);
			await WsClient.SendBinaryAsync(filePath, metadata, chunkSize);
		}
		catch (Exception e)
		{
			Logger(e, "");
		}
    }
	public async Task SendWebSocketBinary(KeyValuePair<string,string> fileValuePair, int chunkSize = 64 * 1024)
	{
		var directoryPrefix = fileValuePair.Key;
		var filePath = fileValuePair.Value;
	    var fileInfo = new FileInfo(filePath);
	    var totalChunks = (int)Math.Ceiling((double)fileInfo.Length / chunkSize);

	    Logger(null, $"INFO: Sending binary file \"{fileInfo.Name}\"");
	    // Send Metadata (as text message)
	    var metadata = new Arma3PayloadBinary
	    (
		    fileInfo.Name,
		    fileInfo.Length,
		    fileInfo.CreationTime,
		    totalChunks,
		    directoryPrefix
	    );
		var metaJson = JsonSerializer.Serialize(metadata, Arma3PayloadJsonSerializerContext.Default.Arma3Payload);

		try
		{
			await SendWebSocketMessage(metaJson);
			await WsClient.SendBinaryAsync(filePath, metadata, chunkSize);
		}
		catch (Exception e)
		{
			Logger(e, "");
		}
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
					AccessName =  AccessName,
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
				content : new StringContent(
					jsonPayload,
					Encoding.UTF8, "application/json"
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
			
			//- Establish Socket Connection
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
