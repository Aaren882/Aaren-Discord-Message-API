using Arma3WebService.DBContext.Schema;
using System.Text.Json.Serialization;
using Arma3WebService.DBContext;
using Arma3WebService.Managers;
using Arma3WebService.Models;
using Components.Entity;
using Microsoft.EntityFrameworkCore;
using Arma3WebService.DBContext.Entity;

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
	string? MessageId,
	List<string>? ProfileDateOffsets,
	Arma3ClientProfileConfiguration Configuration
) : IdentityEntity
{
	public override async Task<string> Run(IdentityRolesPayload payload, IServiceProvider serviceProvider, ServiceDbContext dbContext)
	{
		var discordBotService = serviceProvider.GetRequiredService<IDiscordBotService>();

		var profileName = payload.Identity.AccessName;
		var exist = await dbContext.ServerIdentities.FirstOrDefaultAsync(
			o => o.profileName == profileName
		);

		var messageId = string.IsNullOrEmpty(MessageId)
			? exist?.messageId ?? 0 //- check null
			: ulong.Parse(MessageId!);

		var channelId = discordBotService.GetPresetMessageChannelId(DiscordBotChannel.Monitor);
		var channel = await discordBotService.GetMessageChannelAsync(channelId);
		var monitorMessage = messageId is 0
			? null
			: await channel.GetMessageAsync(messageId);

		//- Get message template from DB
		var serverInfoTemplate = await dbContext.ServerInfoList.FirstOrDefaultAsync(
			x => x.messageId == messageId
		);

		if (monitorMessage is null || serverInfoTemplate is null) //- validate discord message
		{
			//- Create a new message (if message for server info is not exist)
			if (serverInfoTemplate != null)
			{
				dbContext.ServerInfoList.Remove(serverInfoTemplate);
				var message = await channel.SendMessageAsync("PLACEHOLDER");
				messageId = message.Id;
			}

			var infoTemplate = Configuration.CreateInfoTemplate(messageId);
			await dbContext.ServerInfoList.AddAsync(infoTemplate);

			//- Update cache for other services
			var remoteStateManager = serviceProvider.GetRequiredService<RemoteStateManager>();
			remoteStateManager.TryUpdateExistingServerInfoTemplateCache(messageId, infoTemplate);
		}

		//- Update Identity
		var isNewIdentity = exist == null;
		var profileLastUpdate = ProfileDateOffsets?.Sum(long.Parse) ?? 0;
		var isDifferent = profileLastUpdate != exist?.profileStateStamp
						  || exist.messageId != messageId
						  || monitorMessage is null
						  || serverInfoTemplate is null;
		if (isNewIdentity)
		{
			//- create new identity
			await dbContext.ServerIdentities.AddAsync(new ServerIdentity
			{
				profileName = profileName,
				messageId = messageId,
				profileStateStamp = profileLastUpdate,
			});
		}
		else if (isDifferent)
		{
			//- Update Property
			exist!.messageId = messageId;
			exist!.lastUpdate = DateTime.Now;
			exist.profileStateStamp = profileLastUpdate;
		}

		if (isNewIdentity || isDifferent)
		{
			await dbContext.SaveChangesAsync();
		}

		//- this is used for extension callback
		return $"[\"{profileName}\",\"{messageId}\",{isNewIdentity},{isDifferent}]";
	}
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, AllowOutOfOrderMetadataProperties = true)] // Optional: Add desired options
[JsonSerializable(typeof(IdentityEntity))]
public sealed partial class IdentityEntityJsonSerializerContext : JsonSerializerContext;
