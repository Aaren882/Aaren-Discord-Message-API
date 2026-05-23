using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using JwtRegisteredClaimNames = Microsoft.IdentityModel.JsonWebTokens.JwtRegisteredClaimNames;

namespace Arma3WebService.Identities
{
	public class JwtHelpers
	{
		private readonly IConfiguration Configuration;
		private readonly string issuer;
		private readonly string audience;
		private readonly string signKey;

		public JwtHelpers(IConfiguration configuration)
		{
			Configuration = configuration;
			issuer = Configuration["Jwt:Issuer"]!;
			audience = Configuration["Jwt:Audience"]!;
			signKey = Configuration["Jwt:Key"]!;
		}
		
		public IdentityRolesReturnPayload GenerateToken(IdentityRolesPayload payload)
		{
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

			return new IdentityRolesReturnPayload {
				Identity = payload.Identity,
				RoleName = roleName,
				AuthToken = serializeToken
			};
		}

		public Task<TokenValidationResult> VaildateToken(IdentityRolesPayload payload)
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
				new Claim(JwtRegisteredClaimNames.Sub, payload.AccessName), // Subject Name
				new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID
				new Claim(ClaimTypes.Name, payload.AccessName),
				new Claim(ClaimTypes.Role, roleName),
				new Claim(ClaimTypes.NameIdentifier, roleGuid),
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
}
