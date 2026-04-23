using System.Text;
using System.Text.Json.Serialization;
using Arma3WebService.DBContext;
using Arma3WebService.Models;
using Components.Entity;
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
			service.SendMessageAsync(DiscordMessage);
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
	public override Task Run(IServiceProvider serviceProvider, ServiceDbContext dbContext)
	=> dbContext.UpdateServerIdentityMessageIdAsync(profileName, MessageId);
}

public record UpdateServerInfoTemplateExtension
(
	string MessageId,
	string JsonContent
) : Arma3PayloadExtended
{
	[JsonIgnore]
	public override Arma3PayLoadTypeExtension Type => Arma3PayLoadTypeExtension.UpdateServerInfo;

	public override async Task Run(IServiceProvider serviceProvider, ServiceDbContext dbContext)
	{
		var dbSet = dbContext.UpdateServerInfo;
		var parsedId = ulong.Parse(MessageId); 
		var exist = dbSet.FirstOrDefault(o => o.messageId == parsedId);
		
		var fileInfo = await CreateTemplate();
		
		if (exist == null) {
			await dbSet.AddAsync(new ServerInfoTemplate
			{
				messageId = parsedId,
				filePath = fileInfo.FullName,
				fileCreateTime = fileInfo.CreationTime
			});
		} else {
			exist.filePath = fileInfo.FullName;
			exist.lastUpdate = DateTime.Now;
		}
		
		await dbContext.SaveChangesAsync();
	}

	private async Task<FileInfo> CreateTemplate()
	{
		var file = $".profile/ServerInfoTemplate/{MessageId}.json"; 
		var directory = Path.GetDirectoryName(file);

		if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
		await File.WriteAllTextAsync(file, JsonContent, Encoding.UTF8);

		return new FileInfo(file);
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
