using System.Text.Json.Serialization;
using Arma3WebService.DBContext;
using Components.Entity;
using Microsoft.EntityFrameworkCore;

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
	public virtual Task<string> Run(IdentityRolesPayload payload, IServiceProvider serviceProvider, ServiceDbContext dbContext) => Task.FromResult(string.Empty); 
}

public record ProfileIdentityCheck(
	string MessageId,
	List<string> ProfileDateOffsets
) : IdentityEntity
{
	public override async Task<string> Run(IdentityRolesPayload payload, IServiceProvider serviceProvider, ServiceDbContext dbContext)
	{
		var profileName = payload.Identity.AccessName;
		var messageId = ulong.Parse(MessageId);
		var profileLastUpdate = ProfileDateOffsets.Sum(long.Parse);
		
		var exist = await dbContext.ServerIdentities.FirstOrDefaultAsync(
			o => 
				o.profileName == profileName
		);

		var isNewIdentity = exist == null;
		var isDifferent = profileLastUpdate != exist?.profileStateStamp || exist!.messageId != messageId;
		if (isNewIdentity)
		{
			//- create new identity
			await dbContext.ServerIdentities.AddAsync(new ServerIdentity
			{
				profileName = profileName,
				messageId = ulong.Parse(MessageId),
				profileStateStamp = profileLastUpdate,
			});
		}
		else if (isDifferent)
		{
			//- Update Property
			exist!.messageId = ulong.Parse(MessageId);
			exist!.lastUpdate = DateTime.Now;
			exist.profileStateStamp = profileLastUpdate;
		}

		if (isNewIdentity || isDifferent)
		{
			await dbContext.SaveChangesAsync();
		}

		//- this is used for extension callback
		return $"[\"{profileName}\",\"{MessageId}\",{isNewIdentity},{isDifferent}]";
	}
}

/*public record ProfileIdentityEntity(
	string MessageId,
	string MessageContent
) : IdentityEntity
{
	public override async Task<string> Run(IdentityRolesPayload payload, IServiceProvider serviceProvider, ServiceDbContext dbContext)
	{
		var profileName = payload.Identity.AccessName;
		
		var isNewIdentity = await dbContext.CreateServerIdentityAsync(profileName, MessageId);
		var serverIdentity = await dbContext.GetServerIdentityMessageIdAsync(profileName);
		
		//- Make sure template is updated 
		await new UpdateServerInfoTemplateExtension(MessageId, MessageContent)
			.Run(serviceProvider, dbContext);
		
		//- this is use for extension callback
		return $"[\"{profileName}\",{isNewIdentity},\"{MessageId}\"]";
	}
}*/

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, AllowOutOfOrderMetadataProperties = true)] // Optional: Add desired options
[JsonSerializable(typeof(IdentityEntity))]
public sealed partial class IdentityEntityJsonSerializerContext : JsonSerializerContext;
