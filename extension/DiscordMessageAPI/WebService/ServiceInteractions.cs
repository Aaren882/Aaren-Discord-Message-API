using System.Data;
using System.Net.Http.Headers;
using System.Text;
using Arma3WebService;
using Arma3WebService.Identities;
using DiscordMessageAPI.Tools;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace DiscordMessageAPI.WebService;

public class ServiceInteractions()
{
	private const string Secret = "secret.json"; 
	private static readonly Arma3ServiceSecret ServiceSecret = GetServiceSecret();
	private static string AccessName;

	public event Action<IdentityRolesReturnPayload>? AccessTokenReceived;
	private readonly WebSocketClient WsClient = new(ServiceSecret.WebSocketServiceUri + "/api/ws/ingame");

	/// <summary>
	/// This method securely authenticates with a backend service using credentials from a configuration file to obtain a temporary access token for making further API calls.
	/// </summary>
	private async Task<IdentityRolesReturnPayload?> GetAccessToken(string accessName)
	{
		try
		{
			if (string.IsNullOrEmpty(AccessName))
				AccessName = accessName;
			
			//- Send Request for access token
			var payload = new IdentityRolesPayload
			{
				Name = AccessName,
				Role = Role.GameServer,
				ExpireMinute = 15
			};
			var jsonPayload = JsonSerializer.Serialize(
				payload,
				IdentityRolesPayload_JsonSerializerContext.Default.IdentityRolesPayload
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
				IdentityRolesPayload_JsonSerializerContext.Default.IdentityRolesReturnPayload
			)!;

			if (authTokenPayload == null)
				throw new NullReferenceException($"{nameof(authTokenPayload)} is null.");
			
			Logger.Trace("Token Manager (Token)", authTokenPayload.AuthToken);
			Logger.Trace("Token Manager (Role Name)", authTokenPayload.RoleName);
			
			//- Establish Socket Connection
			AccessTokenReceived?.Invoke(authTokenPayload);
			
			return authTokenPayload;
		}
		catch (Exception e)
		{
			Logger.Log(e);
			return null;
		}
	}
	internal async Task EstablishWebSocketConnection(string accessName)
	{
		var tokenPayload = await GetAccessToken(accessName);
		await WsClient.ConnectAsync(tokenPayload.AuthToken);
	}
	internal async Task DisconnectWebSocket()
	{
		await WsClient.DisconnectAsync();
	}
	internal async Task ReconnectWebSocket()
	{
		await DisconnectWebSocket();
		await EstablishWebSocketConnection(AccessName);
	}
	internal async Task SendWebSocketMessage(Arma3Payload messageObj)
	{
		var context = messageObj.MessageType switch
		{
			Arma3PayLoadType.Logging => Arma3Payload_JsonSerializerContext.Default.Arma3Payload,
			_ => null
		};
		
		if (context == null)
			throw new NoNullAllowedException("Websocket message context is not exist.");
		
		var messageJson = JsonSerializer.Serialize(messageObj, context);
		await WsClient.SendMessageAsync(messageJson);
	}
	
	private static Arma3ServiceSecret GetServiceSecret()
	{
		var secretString = Util.ParseJson(Secret);
		var tokenPayload = JsonSerializer.Deserialize(
			secretString,
			Arma3Payload_JsonSerializerContext.Default.Arma3ServiceSecret
		)!;
		
		Logger.Trace("GetServiceSecret", secretString);
		Logger.Trace("GetServiceSecret (Username)", tokenPayload.Secret.Username);
		Logger.Trace("GetServiceSecret (Password)", tokenPayload.Secret.Password);
		Logger.Trace("GetServiceSecret (Uri)", tokenPayload.ServiceUri);
		Logger.Trace("GetServiceSecret (WS Uri)", tokenPayload.WebSocketServiceUri);

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
