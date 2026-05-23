using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Arma3WebService.DBContext;
using Arma3WebService.Entity;
using Components.Entity;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Arma3WebService.Identities;
public sealed class JwtHelpers(
	IConfiguration configuration,
	IServiceProvider serviceProvider,
	ILogger<JwtHelpers> logger,
	ServiceDbContext dbContext
)
{
	private readonly string issuer = configuration["Jwt:Issuer"]!;
	private readonly string audience = configuration["Jwt:Audience"]!;
	private readonly string signKey = configuration["Jwt:Key"]!;
	
	public IdentityRolesReturnPayload GenerateToken(IdentityRolesPayload payload)
	{
		var accessName = payload.Identity.AccessName;
		var roleName = GetIdentityRole(payload.Identity);
		var userClaimsIdentity = CreateClaimsIdentity(payload.Identity);

		// Symmetric Key for Credential
		var secret = GenerateHashSecret(signKey);
		var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

		// HmacSha256 MUST be larger than 128 bits, so the key can't be too short. At least 16 and more characters.
		var signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256Signature);

		// Create SecurityTokenDescriptor
		var tokenDescriptor = new SecurityTokenDescriptor
		{
			Issuer = issuer,
			Audience = roleName,
			Subject = userClaimsIdentity,

			Expires = payload.ExpireMinute is null
				? null
				: DateTime.Now.AddMinutes((int)payload.ExpireMinute!),

			SigningCredentials = signingCredentials
		};

		// Create Token
		var tokenHandler = new JwtSecurityTokenHandler();
		var securityToken = tokenHandler.CreateToken(tokenDescriptor);
		var serializeToken = tokenHandler.WriteToken(securityToken);
		
		logger.LogInformation("\"{accessName}\" is using JWT.",accessName);
		
		//- Get additional info in database
		if (string.IsNullOrEmpty(payload.AdditionalPayload))
			return new IdentityRolesReturnPayload
			{
				Identity = payload.Identity,
				RoleName = roleName,
				AuthToken = serializeToken,
			};
		
		
		var identityPayload = JsonSerializer.Deserialize(
			payload.AdditionalPayload, 
			IdentityEntityJsonSerializerContext.Default.IdentityEntity
		);
		
		var additionalPayload = identityPayload?.Run(payload, serviceProvider, dbContext)
			.GetAwaiter().GetResult();
		
		logger.LogInformation("Additional Payload (Result) : \"{additionalPayload}\"",additionalPayload);

		return new IdentityRolesReturnPayload {
			Identity = payload.Identity,
			RoleName = roleName,
			AuthToken = serializeToken,
			AdditionalPayload = additionalPayload
		};
	}

	public Task<TokenValidationResult> ValidateToken(IdentityRolesPayload payload)
	{
		var tokenHandler = new JsonWebTokenHandler();
		return tokenHandler.ValidateTokenAsync(payload.AuthToken, GetValidationParameters());
	}
	
	internal TokenValidationParameters GetValidationParameters()
	{
		// Symmetric Key for Credential
		var secret = GenerateHashSecret(signKey);
		var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));

		return new TokenValidationParameters
		{
			// the token's issuer
			ValidateIssuer = true,
			ValidIssuer = issuer,
			// Recipient
			ValidateAudience = false,
			//ValidAudience = GetIdentityRole(role),

			// Token Life Time
			ValidateLifetime = true,
			// vaildate when the key is in the token. or just check the signature
			ValidateIssuerSigningKey = false,

			//RoleClaimType = GetIdentityRole(role),
			// key
			IssuerSigningKey = securityKey
		};
	}
	private static string GenerateHashSecret(string input)
	{
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
		return Convert.ToBase64String(hash);
	}
	private static ClaimsIdentity CreateClaimsIdentity(IdentityInfo payload)
	{
		var roleName = GetIdentityRole(payload);
		var roleGuid = GetIdentityRoleGuid(payload);

		var claims = new List<Claim>{
			new (JwtRegisteredClaimNames.Sub, payload.AccessName), // Subject Name
			new (JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID
			new (ClaimTypes.Name, payload.AccessName),
			new (ClaimTypes.Role, roleName),
			new (ClaimTypes.NameIdentifier, roleGuid),
		};

		return new ClaimsIdentity(claims);
	}
	private static string GetIdentityRole(IdentityInfo identity)
	{
		return identity switch
		{
			{ Role: Role.Admin } => IdentityRoles.Admin,
			{ Role: Role.GameServer } => IdentityRoles.GameServer,
			_ => throw new ArgumentOutOfRangeException("Request Role is not supported.")
		};
	}
	private static string GetIdentityRoleGuid(IdentityInfo identity)
	{
		return identity switch
		{
			{ Role: Role.Admin } => IdentityRoles.AdminGuid.ToString(),
			{ Role: Role.GameServer } => IdentityRoles.GameServerGuid.ToString(),
			_ => throw new ArgumentOutOfRangeException("Request Role is not supported.")
		};
	}
}
