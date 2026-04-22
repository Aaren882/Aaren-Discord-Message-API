using System.Security.Cryptography;
using System.Text;
using Arma3WebService.Identities;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Arma3WebService.Configuration;

public sealed class JwtConfigureOptions(JwtHelpers jwtHelpers) : IConfigureNamedOptions<JwtBearerOptions>
{
	public void Configure(string name, JwtBearerOptions options)
	{
		// Only configure for the default JwtBearer scheme
		if (name == JwtBearerDefaults.AuthenticationScheme)
		{
			options.TokenValidationParameters = jwtHelpers.GetValidationParameters();
		}
	}

	public void Configure(JwtBearerOptions options) => Configure(Options.DefaultName, options);
}
