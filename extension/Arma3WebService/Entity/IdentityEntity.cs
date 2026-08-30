using System.Text.Json.Serialization;
using Arma3WebService.DBContext.Entity;
using Arma3WebService.Models;
using Components.Entity;

namespace Arma3WebService.Entity;

/*public enum ProfileIdentity
{
	Admin = 1,
	GameServer = 2,
}*/

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(IdentityEntity), (int)Role.Admin)]
[JsonDerivedType(typeof(ProfileIdentityCheck), (int)Role.GameServer)]
public record IdentityEntity
{
	public virtual Task<string> Run(IdentityRolesPayload payload, IServiceProvider serviceProvider) => Task.FromResult(string.Empty);
}

public record ProfileIdentityCheck(
	string? MessageId,
	List<string>? ProfileDateOffsets,
	Arma3ClientProfileConfiguration Configuration
) : IdentityEntity
{
	public override async Task<string> Run(IdentityRolesPayload payload, IServiceProvider serviceProvider)
	{
		var identityCheckService = serviceProvider.GetRequiredService<IdentityCheckService>();
		var (result, _, _) = await identityCheckService.ProcessProfileCheckAsync(payload, this);
		return result;
	}
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, AllowOutOfOrderMetadataProperties = true)] // Optional: Add desired options
[JsonSerializable(typeof(IdentityEntity))]
public sealed partial class IdentityEntityJsonSerializerContext : JsonSerializerContext;
