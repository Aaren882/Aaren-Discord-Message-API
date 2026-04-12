using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using Components.Entity;
using DiscordMessageAPI.ServiceConnection.WebService;
using ServiceConnection.Tools;
using System.Text.Json;
using static ServiceConnection.ServiceStartup;

namespace ServiceConnection.WebService;

public class ServiceInteractions
{
	private const string Secret = "secret.json"; 
	private static readonly Arma3ServiceSecret ServiceSecret = GetServiceSecret();
	
	private string? AccessName;
	public readonly string RPTDirectory = Path.GetFullPath(ServiceSecret.RPT_Directory);

	public event Action<IdentityRolesReturnPayload>? AccessTokenReceived;
	private readonly WebSocketClient _wsClient = new (ServiceSecret.WebSocketServiceUri + "/api/ws/ingame");

	public ServiceInteractions()
	{
		_wsClient.Connected += () =>
		{
			Logger(null, "INFO: Connected to server");
			
			var callBack = new Arma3PayloadCallBack(
				Data : "[true]",
				Function : "ConnectionChanged"
			);
			Util.CallExtensionCallback(Callback, callBack);
		};
		_wsClient.Disconnected += () =>
		{
			Logger(null, "INFO: Disconnected from server");
			
			var callBack = new Arma3PayloadCallBack(
				Data : "[false]",
				Function : "ConnectionChanged"
			);
			Util.CallExtensionCallback(Callback, callBack);
		};
		_wsClient.MessageReceived += (message) =>
		{
			Tracer("MessageReceived (message)", message.ToString());
			Util.CallExtensionCallback(Callback, message);
		};
	}
	
	public async Task EstablishWebSocketConnection(string? accessName)
	{
		if (_wsClient.Status() == WebSocketState.Open)
		{
			Logger(null, "WebSocket connection already established.");
			return;
		}
		
		var tokenPayload = await GetAccessToken(accessName);
		await _wsClient.ConnectAsync(tokenPayload.AuthToken);
	}
	public async Task DisconnectWebSocket(string description = "Client disconnect")
	{
		await _wsClient.DisconnectAsync(description);
	}
	public async Task ReconnectWebSocket()
	{
		await DisconnectWebSocket("Client Reconnecting");
		await EstablishWebSocketConnection(AccessName);
	}
	public Task SendWebSocketMessage(string messageJson)
	{
		return _wsClient.SendMessageAsync(messageJson);
	}
	public async Task SendWebSocketBinary(string filePath, int chunkSize = 64 * 1024)
    {
	    var fileInfo = new FileInfo(filePath);
	    var totalChunks = (int)Math.Ceiling((double)fileInfo.Length / chunkSize);

	    // Send Metadata (as text message)
	    var metadata = new Arma3PayloadRPT
	    (
		    fileInfo.Name,
		    fileInfo.Length,
		    fileInfo.CreationTime,
		    totalChunks
	    );
		var metaJson = JsonSerializer.Serialize(metadata, Arma3PayloadJsonSerializerContext.Default.Arma3Payload);

		try
		{
			await SendWebSocketMessage(metaJson);
			await _wsClient.SendBinaryAsync(filePath, metadata, chunkSize);
		}
		catch (Exception e)
		{
			Logger(e, "");
		}
    }
	
	/// <summary>
	/// This method securely authenticates with a backend service using credentials from a configuration file to obtain a temporary access token for making further API calls.
	/// </summary>
	private async Task<IdentityRolesReturnPayload> GetAccessToken(string accessName)
	{
		try
		{
			if (string.IsNullOrEmpty(AccessName))
				AccessName = accessName;
			
			//- Send Request for access token
			var payload = new IdentityRolesPayload
			{
				Identity = new IdentityInfo
				{
					AccessName =  AccessName,
					Role = Role.GameServer
				},
				ExpireMinute = 15
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

			if (authTokenPayload is { AuthToken: null })
				throw new NullReferenceException($"{nameof(authTokenPayload)} is null.");
			
			Tracer("Token Manager", authTokenPayload.ToString());
			
			//- Establish Socket Connection
			AccessTokenReceived?.Invoke(authTokenPayload);
			
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
