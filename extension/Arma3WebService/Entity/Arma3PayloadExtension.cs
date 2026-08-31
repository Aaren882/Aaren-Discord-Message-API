using System.Text.Json.Serialization;
using Arma3WebService.DBContext.Entity;
using Arma3WebService.DBContext.Repositories;
using Arma3WebService.Managers;
using Arma3WebService.Models;
using Component.DiscordEntity;
using Discord;

namespace Arma3WebService.Entity;

public enum Arma3PayLoadTypeExtension
{
	DiscordSend = 1,
	UpdateServerIdentity = 2,
	UpdateServerInfo = 3,
	RegisterServerIdentity = 4,
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "ProcessType")]
[JsonDerivedType(typeof(DiscordJsonExtension), (int)Arma3PayLoadTypeExtension.DiscordSend)]
[JsonDerivedType(typeof(UpdateServerIdentityExtension), (int)Arma3PayLoadTypeExtension.UpdateServerIdentity)]
[JsonDerivedType(typeof(UpdateServerInfoTemplateExtension), (int)Arma3PayLoadTypeExtension.UpdateServerInfo)]
[JsonDerivedType(typeof(RegisterServerIdentity), (int)Arma3PayLoadTypeExtension.RegisterServerIdentity)]
public abstract record Arma3PayloadExtended
{
	public abstract Arma3PayLoadTypeExtension Type { get; }
	public static DateTime Timestamp => DateTime.Now;
	public abstract Task Run(IServiceProvider serviceProvider, IServerIdentityRepository identityRepository, IServerInfoTemplateRepository infoRepository);
}

public record DiscordJsonExtension
(
	DiscordMessageDto DiscordMessage,
	string MessageId = ""
) : Arma3PayloadExtended
{
	[JsonIgnore]
	public override Arma3PayLoadTypeExtension Type => Arma3PayLoadTypeExtension.DiscordSend;

	public override Task Run(IServiceProvider serviceProvider, IServerIdentityRepository identityRepository, IServerInfoTemplateRepository infoRepository)
	{
		var service = serviceProvider.GetRequiredService<IDiscordBotService>();
		return SendMessage(service);
	}

	private Task<IUserMessage> SendMessage(IDiscordBotService service)
	{
		return ulong.TryParse(MessageId, out var id) ?
			service.ModifyMessageAsync(id, DiscordMessage) :
			service.SendMessageAsync(
				service.GetPresetMessageChannelId(DiscordBotChannel.Monitor),
				DiscordMessage);
	}
}

public record UpdateServerIdentityExtension
(
	string profileName,
	string MessageId
) : Arma3PayloadExtended
{
	[JsonIgnore]
	public override Arma3PayLoadTypeExtension Type => Arma3PayLoadTypeExtension.UpdateServerIdentity;
	public override async Task Run(IServiceProvider serviceProvider, IServerIdentityRepository identityRepository, IServerInfoTemplateRepository infoRepository)
	{
		await identityRepository.UpdateServerIdentityMessageIdAsync(profileName, MessageId);
		await identityRepository.DbContext.SaveChangesAsync();

		var messageId = ulong.Parse(MessageId);
		var remoteStateManager = serviceProvider.GetRequiredService<RemoteStateManager>();
		remoteStateManager.TryUpdateServerInfoMessageId(profileName, messageId);
	}
}

public record UpdateServerInfoTemplateExtension
(
	string MessageId,
	Arma3ClientProfileConfiguration Configuration
) : Arma3PayloadExtended
{
	[JsonIgnore]
	public override Arma3PayLoadTypeExtension Type => Arma3PayLoadTypeExtension.UpdateServerInfo;

	public override async Task Run(IServiceProvider serviceProvider, IServerIdentityRepository identityRepository, IServerInfoTemplateRepository infoRepository)
	{
		var messageId = ulong.Parse(MessageId);

		var existIdentity = await identityRepository.GetByMessageIdAsync(messageId);
		ArgumentNullException.ThrowIfNull(existIdentity);

		// Use the repository to handle fetching and creating/updating
		// var (updated, existIdentity) = await infoRepository.GetOrCreateTemplateAndIdentityAsync(messageId, Configuration);
		var updated = await infoRepository.GetOrCreateTemplateAsync(messageId, Configuration);
		await infoRepository.DbContext.SaveChangesAsync();

		// Update cache for other services
		var remoteStateManager = serviceProvider.GetRequiredService<RemoteStateManager>();
		remoteStateManager.TryUpdateExistingServerInfoTemplateCache(messageId, updated);
		remoteStateManager.TryUpdateServerInfoMessageId(existIdentity.profileName, messageId);
	}
}


public record RegisterServerIdentity(UpdateServerIdentityExtension Identity, UpdateServerInfoTemplateExtension InfoTemplate) : Arma3PayloadExtended
{
	[JsonIgnore]
	public override Arma3PayLoadTypeExtension Type => Arma3PayLoadTypeExtension.RegisterServerIdentity;

	public override async Task Run(IServiceProvider serviceProvider, IServerIdentityRepository identityRepository, IServerInfoTemplateRepository infoRepository)
	{
		// The dependency injection framework is responsible for resolving the correct repositories
		await Identity.Run(serviceProvider, identityRepository, infoRepository);
		await InfoTemplate.Run(serviceProvider, identityRepository, infoRepository);
	}
};

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, AllowOutOfOrderMetadataProperties = true)] // Optional: Add desired options
[
	JsonSerializable(typeof(List<Arma3PayloadExtended>)),
	JsonSerializable(typeof(Arma3PayloadExtended))
]
public sealed partial class Arma3PayloadExtendedJsonSerializerContext : JsonSerializerContext;
