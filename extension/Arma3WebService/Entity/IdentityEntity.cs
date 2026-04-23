using System.Text.Json.Serialization;
using Arma3WebService.DBContext;
using Components.Entity;

namespace Arma3WebService.Entity;

/*public enum ProfileIdentity
{
	Admin = 1,
	GameServer = 2,
}*/

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(IdentityEntity), (int)Role.Admin)]
[JsonDerivedType(typeof(ProfileIdentityEntity), (int)Role.GameServer)]
public record IdentityEntity()
{
	public virtual Task<string> Run(IdentityRolesPayload payload, IServiceProvider serviceProvider, ServiceDbContext dbContext)
		=> Task.FromResult(string.Empty); 
}

public record ProfileIdentityEntity(
	string MessageId,
	string MessageContent
) : IdentityEntity
{
	public override async Task<string> Run(IdentityRolesPayload payload, IServiceProvider serviceProvider, ServiceDbContext dbContext)
	{
		var profileName = payload.Identity.AccessName;
		
		var isNewIdentity = await dbContext.CreateServerIdentityAsync(profileName, MessageId);
		var serverIdentity = await dbContext.GetServerIdentityMessageIdAsync(profileName);

		var messageId = serverIdentity?.messageId.ToString() ?? "";
		
		//- Make sure template is updated 
		await new UpdateServerInfoTemplateExtension(MessageId, MessageContent)
			.Run(serviceProvider, dbContext);
		
		//- this is use for extension callback
		return $"[\"{profileName}\",{isNewIdentity},\"{messageId}\"]";
	}
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, AllowOutOfOrderMetadataProperties = true)] // Optional: Add desired options
[JsonSerializable(typeof(IdentityEntity))]
public sealed partial class IdentityEntityJsonSerializerContext : JsonSerializerContext;
