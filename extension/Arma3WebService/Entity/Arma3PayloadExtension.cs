using System.Text;
using System.Text.Json.Serialization;
using Arma3WebService.DBContext;
using Arma3WebService.Managers;
using Arma3WebService.Models;
using Components.Entity;
using Discord;
using Microsoft.EntityFrameworkCore;

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
	public virtual Task Run(IServiceProvider serviceProvider, ServiceDbContext dbContext) => Task.CompletedTask;
}

public record DiscordJsonExtension
(
	DiscordMessageDto DiscordMessage,
	string MessageId = ""
) : Arma3PayloadExtended
{
	[JsonIgnore]
	public override Arma3PayLoadTypeExtension Type => Arma3PayLoadTypeExtension.DiscordSend;
	
	public override async Task Run(IServiceProvider serviceProvider, ServiceDbContext dbContext)
	{
		var service = serviceProvider.GetRequiredService<IDiscordBotService>();
		await SendMessage(service);
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
	public override async Task Run(IServiceProvider serviceProvider, ServiceDbContext dbContext)
	{
		await dbContext.UpdateServerIdentityMessageIdAsync(profileName, MessageId);
		
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

	public override async Task Run(IServiceProvider serviceProvider, ServiceDbContext dbContext)
	{
		var messageId = ulong.Parse(MessageId);
		var exist = await dbContext.ServerInfoList.FirstOrDefaultAsync(x => x.messageId == messageId);
		var updated = Configuration.CreateInfoTemplate(messageId);
		
		if (exist == null)
		{
			await dbContext.ServerInfoList.AddAsync(updated);
		}
		else
		{
			dbContext.Entry(exist).CurrentValues.SetValues(updated);
		}
		
		await dbContext.SaveChangesAsync();
		
		//- Update cache for other services
		var remoteStateManager = serviceProvider.GetRequiredService<RemoteStateManager>();
		remoteStateManager.TryUpdateExistingServerInfoTemplateCache(messageId, exist!);
	}
	/*public override async Task Run(IServiceProvider serviceProvider, ServiceDbContext dbContext)
	{
		var fileInfo = await CreateTemplate();
		await dbContext.UpsertServerInfoTemplateAsync(fileInfo, MessageId);
	}
	private async Task<FileInfo> CreateTemplate()
	{
		var file = $".profile/ServerInfoTemplate/{MessageId}.json";
		var directory = Path.GetDirectoryName(file);

		if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
		await File.WriteAllTextAsync(file, JsonContent, Encoding.UTF8);

		return new FileInfo(file);
	}*/
}

public record struct Arma3ClientProfileConfiguration
{
	private FileInfo? _messageTemplate;
	private FileInfo? _messageOfflineTemplate;
	private FileInfo? _messageActions;

	public string? MessageTemplate
	{
		get => _messageTemplate?.Exists ?? false ? _messageTemplate.FullName : null;
		set {
			if (value is not null) {
				_messageTemplate = new FileInfo(
					Path.GetFullPath($".profile/MessageTemplate/{Path.GetFileName(value)}")
				);
			}
		}
	}
	
	public string? MessageOfflineTemplate
	{
		get => _messageOfflineTemplate?.Exists ?? false ? _messageOfflineTemplate.FullName : null;
		set {
			if (value is not null) {
				_messageOfflineTemplate = new FileInfo(
					Path.GetFullPath($".profile/MessageOfflineTemplate/{Path.GetFileName(value)}")
				);
			}
		}
	}

	public string? MessageActions
	{
		get => _messageActions?.Exists ?? false ? _messageActions.FullName : null;
		set => _messageActions = new FileInfo(
			Path.GetFullPath($".profile/MessageActions/{Path.GetFileName(value)}")
		);
	}

	public ServerInfoTemplate CreateInfoTemplate(ulong messageId)
	{
		return new ServerInfoTemplate
		{
			messageId = messageId,
			messageTemplatePath = MessageTemplate,
			messageOfflinePath = MessageOfflineTemplate,
			messageActionPath = MessageActions,
		};
	}
}

public record RegisterServerIdentity
(
	UpdateServerIdentityExtension Identity, //- Setup Message ID for profile
	UpdateServerInfoTemplateExtension InfoTemplate //- Acquire JSON message template
) : Arma3PayloadExtended
{
	[JsonIgnore]
	public override Arma3PayLoadTypeExtension Type => Arma3PayLoadTypeExtension.RegisterServerIdentity;

	public override async Task Run(IServiceProvider serviceProvider, ServiceDbContext dbContext)
	{
		foreach (var task in (IEnumerable<Arma3PayloadExtended>)[InfoTemplate, Identity])
			await task.Run(serviceProvider, dbContext);
	}
};

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, AllowOutOfOrderMetadataProperties = true)] // Optional: Add desired options
[
	JsonSerializable(typeof(List<Arma3PayloadExtended>)),
	JsonSerializable(typeof(Arma3PayloadExtended))
]
public sealed partial class Arma3PayloadExtendedJsonSerializerContext : JsonSerializerContext;
