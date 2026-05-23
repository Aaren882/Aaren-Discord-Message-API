using System.Text.Json.Serialization;
using Arma3WebService.DBContext;
using Arma3WebService.Managers;
using Arma3WebService.Models;
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

		if (monitorMessage is null) //- Create a new message
		{
			var serverInfoTemplate = await dbContext.ServerInfoList.FirstOrDefaultAsync(
				x => x.messageId == messageId
			);

			if (serverInfoTemplate != null)
			{
				dbContext.ServerInfoList.Remove(serverInfoTemplate);
			}
			
			//- New message
			var message = await channel.SendMessageAsync("PLACEHOLDER");
			messageId = message.Id;
			
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
		                  || monitorMessage is null;
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

/*public record ProfileIdentityEntity(
	string MessageId,
	List<string>? ProfileDateOffsets
) : IdentityEntity
{
	public override async Task<string> Run(IdentityRolesPayload payload, IServiceProvider serviceProvider, ServiceDbContext dbContext)
	{
		var profileName = payload.Identity.AccessName;
		var messageId = ulong.Parse(MessageId);
		
		var exist = await dbContext.ServerIdentities.FirstOrDefaultAsync(
			o => 
				o.profileName == profileName
		);
		
		var isNewIdentity = exist == null;

		var profileLastUpdate = ProfileDateOffsets?.Sum(long.Parse) ?? 0;
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
}*/

/*public record ProfileIdentityNoMessageIdEntity(
	string? MessageId,
	List<string>? ProfileDateOffsets
) : IdentityEntity
{
	public override async Task<string> Run(IdentityRolesPayload payload, IServiceProvider serviceProvider, ServiceDbContext dbContext)
	{
		var discordBotService = serviceProvider.GetRequiredService<IDiscordBotService>();
		/*var channelId = discordBotService.GetPresetMessageChannelId(DiscordBotChannel.AdminConsole);
		var channel = await discordBotService.GetMessageChannelAsync(channelId);

		var titleDisplay = new TextDisplayBuilder("# Create new message for game session");
		var descriptionDisplay = new TextDisplayBuilder($"Session : `{profileName}`");
		var actionRow = new ActionRowBuilder()
			.WithButton("Create", "create")
			.WithButton("Cancel", "cancel");
		
		var container = new ContainerBuilder([titleDisplay, descriptionDisplay, actionRow])
			.WithAccentColor(703487);
		var componentBuilder = new ComponentBuilderV2()
			.WithContainer(container);
		
		await channel.SendMessageAsync(components: componentBuilder.Build());#1#
		
		var channelId = discordBotService.GetPresetMessageChannelId(DiscordBotChannel.Monitor);
		var channel = await discordBotService.GetMessageChannelAsync(channelId);
		
		var message = await channel.SendMessageAsync("PLACEHOLDER");
		var identityEntity = new ProfileIdentityEntity(message.Id.ToString(), ProfileDateOffsets);
		
		return await identityEntity.Run(payload, serviceProvider, dbContext);
	}
}*/

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, AllowOutOfOrderMetadataProperties = true)] // Optional: Add desired options
[JsonSerializable(typeof(IdentityEntity))]
public sealed partial class IdentityEntityJsonSerializerContext : JsonSerializerContext;
