using System.Text.Json.Serialization;
using Arma3WebService.DBContext.Entity;
using Arma3WebService.Models;
using Components.Entity;

namespace Arma3WebService.Entity;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ProfileIdentityCheck), (int)Role.GameServer)]
public abstract record IdentityEntity
{
	public abstract Task<string> Run(IdentityRolesPayload payload, IServiceProvider serviceProvider);
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
