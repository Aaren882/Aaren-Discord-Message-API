using System.Data;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Components.Entity;
using DiscordMessageAPI.ServiceConnection.WebService;
using ServiceConnection.Tools;
using Arma3PayloadJsonSerializerContext = Components.Entity.Arma3PayloadJsonSerializerContext;
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
		_wsClient.Connected += () => Logger(null, "Event: Connected to server");
		_wsClient.Disconnected += () => Logger(null, "Event: Disconnected from server");
		_wsClient.MessageReceived += (message) =>
		{
			// if (message is null) return;
			Tracer("MessageReceived (message)", message.ToString());
			switch (message.MessageType)
			{
				case Arma3PayLoadType.Command: { //- Remote command from websocket
					Util.CallExtensionCallback(Callback, message as Arma3PayloadCallBack);
					break;
				}
				case Arma3PayLoadType.Rpt: //- Remote command to Send RPT
					break;
				case Arma3PayLoadType.Message:
					break;
				// default : throw new Exception("No callBack action is found.");
			}
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
	public Task SendWebSocketMessage(Arma3Payload messageObj)
	{
		var context = messageObj.MessageType switch
		{
			Arma3PayLoadType.Message => Arma3PayloadJsonSerializerContext.Default.Arma3Payload,
			_ => null
		};
		
		if (context == null)
			throw new NoNullAllowedException("Websocket message context is not exist.");
		
		var messageJson = JsonSerializer.Serialize(messageObj, context);
		return _wsClient.SendMessageAsync(messageJson);
	}
	public async Task SendWebSocketBinary(string filePath)
    {
		await _wsClient.SendBinaryAsync(filePath);
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
