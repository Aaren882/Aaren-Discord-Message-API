using Arma3WebService.Broker;
using Arma3WebService.DBContext;
using Arma3WebService.DBContext.Repositories;
using Arma3WebService.Entity;
using Arma3WebService.Managers;
using Components.Entity;
using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.Models;

public class IdentityCheckService(
	ServiceDbContext dbContext,
	IServerIdentityRepository identityRepository,
	IServerInfoTemplateRepository infoRepository,
	IDiscordBotService discordBotService,
	RemoteStateManager remoteStateManager,
	BinaryPayloadBroker binaryPayloadBroker,
	ILogger<IdentityCheckService> logger
)
{
	public async Task<(string Result, bool IsNewIdentity, bool IsDifferent)> ProcessProfileCheckAsync(
		IdentityRolesPayload payload, ProfileIdentityCheck profileIdentity)
	{
		var profileName = payload.Identity.AccessName;
		var (MessageId, ProfileDateOffsets, Configuration) = profileIdentity;
		try
		{
			// The repository methods must now accept the 'transaction' parameter!
			var exist = await identityRepository.GetByProfileNameAsync(profileName);

			var messageId = string.IsNullOrEmpty(profileIdentity.MessageId)
				? exist?.messageId ?? 0
				: ulong.Parse(profileIdentity.MessageId!);

			var channelId = discordBotService.GetPresetMessageChannelId(DiscordBotChannel.Monitor);
			var channel = await discordBotService.GetMessageChannelAsync(channelId);
			var monitorMessage = messageId is 0 ? null : await channel.GetMessageAsync(messageId);

			var serverInfoTemplate = await infoRepository.GetByMessageIdAsync(messageId);

			// Handle message creation/cleanup
			if (monitorMessage is null || serverInfoTemplate is null)
			{
				if (serverInfoTemplate != null)
				{
					// The repository now tracks the removal, but does NOT save it.
					await infoRepository.RemoveTemplateAsync(serverInfoTemplate);
					var message = await channel.SendMessageAsync("PLACEHOLDER");
					messageId = message.Id;
				}

				foreach (var templateFileInfo in profileIdentity.Configuration.GetTemplateFileList())
				{
					var actionName = profileName + templateFileInfo.Name;
					binaryPayloadBroker.TryAdd(actionName, async () =>
					{
						try
						{
							// The repository now tracks the creation/update, but does NOT save it.
							var infoTemplate = await infoRepository.GetOrCreateTemplateAsync(messageId, profileIdentity.Configuration);
							remoteStateManager.TryUpdateExistingServerInfoTemplateCache(messageId, infoTemplate);
							await infoRepository.DbContext.SaveChangesAsync();
						}
						finally
						{
							binaryPayloadBroker.TryRemove(actionName); //- Remove after message template updated
						}
					});
				}
			}

			// Update Identity
			var isNewIdentity = exist == null;
			var profileLastUpdate = profileIdentity.ProfileDateOffsets?.Sum(long.Parse) ?? 0;
			var isDifferent = profileLastUpdate != exist?.profileStateStamp
							|| exist.messageId != messageId
							|| monitorMessage is null
							|| serverInfoTemplate is null;

			if (isNewIdentity)
			{
				// The repository tracks the addition, but does NOT save it.
				await identityRepository.AddServerIdentityAsync(new()
				{
					profileName = profileName,
					messageId = messageId,
					profileStateStamp = profileLastUpdate,
				});
			}
			else if (isDifferent)
			{
				exist!.messageId = messageId;
				exist!.lastUpdate = DateTime.Now;
				exist.profileStateStamp = profileLastUpdate;
				// The repository tracks the update, but does NOT save it.
				await identityRepository.UpdateServerIdentityAsync(exist);
			}

			await dbContext.SaveChangesAsync();

			// Return results needed by the calling payload/extension
			return ($"[\"{profileName}\",\"{messageId}\",{isNewIdentity},{isDifferent}]", isNewIdentity, isDifferent);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Error processing identity profile check for {ProfileName}", profileName);
			throw;
		}
	}
}
