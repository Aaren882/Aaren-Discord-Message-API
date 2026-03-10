using System.Net.Http.Headers;
using System.Text;
using Arma3WebService;
using Arma3WebService.Identities;
using DiscordMessageAPI.Tools;
using System.Text.Json;

namespace DiscordMessageAPI.WebService;

public class ServiceInteractions
{
	private const string Secret = "secret.json"; 
	internal static readonly Arma3ServiceSecret ServiceSecret = GetServiceSecret();
	/// <summary>
	/// This is a placeholder for the logic to retrieve or generate the JWT token.
	/// In a production scenario, this might involve an authentication request to the identity server.
	/// </summary>
	public static async Task<IdentityRolesReturnPayload?> GetAccessToken(string name)
	{
		try
		{
			//- Send Request for access token
			var payload = new IdentityRolesPayload { Name = name, Role = Role.GameServer, ExpireMinute = 15 };
			var jsonPayload = JsonSerializer.Serialize(
				payload,
				IdentityRolesPayload_JsonSerializerContext.Default.IdentityRolesPayload
			);

			using var response = await APIRequest.PostRequest(
		       $"{ServiceSecret.ServiceUri}/api/token",
		       content : new StringContent(
						jsonPayload,
						Encoding.UTF8, "application/json"
					),
		       authHeader: new AuthenticationHeaderValue(
				       "Basic",
				       GetBasicAuthenticationBearer()
			       )
		    );
			
			//- Get the Token
			var result = await response.Content.ReadAsStringAsync();
			Logger.Trace("Token Manager (result)", result);
			
			var tokenPayload = JsonSerializer.Deserialize(
				result,
				IdentityRolesPayload_JsonSerializerContext.Default.IdentityRolesReturnPayload
			)!;
			
			Logger.Trace("Token Manager (Token)", tokenPayload.AuthToken);
			Logger.Trace("Token Manager (Role Name)", tokenPayload.RoleName);

			return tokenPayload;
		}
		catch (Exception e)
		{
			Logger.Log(e);
			return null;
		}
	}

	private static Arma3ServiceSecret GetServiceSecret()
	{
		var secretString = File.ReadAllText(Path.Combine(Util.AssemblyPath, Secret));
		var tokenPayload = JsonSerializer.Deserialize(
			secretString,
			Arma3Payload_JsonSerializerContext.Default.Arma3ServiceSecret
		)!;
		
		Logger.Trace("GetServiceSecret", secretString);
		Logger.Trace("GetServiceSecret (Username)", tokenPayload.Secret.Username);
		Logger.Trace("GetServiceSecret (Password)", tokenPayload.Secret.Password);
		Logger.Trace("GetServiceSecret (Uri)", tokenPayload.ServiceUri);

		return tokenPayload;
	}
	private static string GetBasicAuthenticationBearer()
	{
		return ConvertSecretIntoHash(
			ServiceSecret.Secret.Username,
			ServiceSecret.Secret.Password
		);
	}

	private static string ConvertSecretIntoHash(string username, string password)
	{
		var usernamePassword = string.Join(':', [username, password]);
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(usernamePassword));
	}
}
