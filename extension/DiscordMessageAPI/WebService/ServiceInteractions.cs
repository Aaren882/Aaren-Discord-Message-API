using System.Data;
using System.Net.Http.Headers;
using System.Text;
using DiscordMessageAPI.Tools;
using System.Text.Json;
using Arma3WebService;
using Arma3WebService.Identities;

namespace DiscordMessageAPI.WebService;

public class ServiceInteractions
{
	private const string Secret = "secret.json"; 
	private static readonly Arma3ServiceSecret ServiceSecret = GetServiceSecret();
	private static string AccessName;
	internal static readonly string RPTDirectory = Path.GetFullPath(ServiceSecret.RPT_Directory);

	public event Action<IdentityRolesReturnPayload>? AccessTokenReceived;
	private readonly WebSocketClient _wsClient = new(ServiceSecret.WebSocketServiceUri + "/api/ws/ingame");

	public ServiceInteractions()
	{
		_wsClient.Connected += () => Logger.Log(null, "Event: Connected to server");
		_wsClient.Disconnected += () => Logger.Log(null, "Event: Disconnected from server");
		_wsClient.MessageReceived += (message) => Logger.Log(null, $"Event: Message received - {message}");
	}
	
	internal async Task EstablishWebSocketConnection(string accessName)
	{
		var tokenPayload = await GetAccessToken(accessName);
		await _wsClient.ConnectAsync(tokenPayload.AuthToken);
	}
	internal async Task DisconnectWebSocket()
	{
		await _wsClient.DisconnectAsync();
	}
	internal async Task ReconnectWebSocket()
	{
		await DisconnectWebSocket();
		await EstablishWebSocketConnection(AccessName);
	}
	internal Task SendWebSocketMessage(Arma3Payload messageObj)
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
	internal Task SendWebSocketBinary(string filePath)
    {
		return _wsClient.SendBinaryAsync(filePath);
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
			Logger.Trace("Token Manager (result)", result);
			
			var authTokenPayload = JsonSerializer.Deserialize(
				result,
				IdentityRolesPayloadJsonSerializerContext.Default.IdentityRolesReturnPayload
			)!;

			if (authTokenPayload is { AuthToken: null })
				throw new NullReferenceException($"{nameof(authTokenPayload)} is null.");
			
			Logger.Trace("Token Manager", authTokenPayload.ToString());
			
			//- Establish Socket Connection
			AccessTokenReceived?.Invoke(authTokenPayload);
			
			return authTokenPayload;
		}
		catch (Exception e)
		{
			Logger.Log(e);
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
		
		Logger.Trace("GetServiceSecret", secretString);
		Logger.Trace("GetServiceSecret (Username)", tokenPayload.Secret.Username);
		Logger.Trace("GetServiceSecret (Password)", tokenPayload.Secret.Password);
		Logger.Trace("GetServiceSecret (Uri)", tokenPayload.ServiceUri);
		Logger.Trace("GetServiceSecret (WS Uri)", tokenPayload.WebSocketServiceUri);
		Logger.Trace("GetServiceSecret (RPT Directory)", tokenPayload.RPT_Directory);

		return tokenPayload;
	}
	private static string GetBasicAuthenticationBearer(Arma3ServiceSecret serviceSecret)
	{
		return ConvertSecretIntoHash(
			serviceSecret.Secret.Username,
			serviceSecret.Secret.Password
		);
	}
	private static string ConvertSecretIntoHash(string username, string password)
	{
		var usernamePassword = string.Join(':', [username, password]);
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(usernamePassword));
	}
}
