using System.Text;
using System.Text.Json.Serialization;
using Arma3WebService.DBContext;
using Arma3WebService.Models;
using Discord;

namespace Arma3WebService.Entity;

public enum Arma3PayLoadTypeExtension
{
	DiscordSend = 1,
	UpdateServerInfo = 2,
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "ProcessType")]
[JsonDerivedType(typeof(DiscordJsonExtension), (int)Arma3PayLoadTypeExtension.DiscordSend)]
[JsonDerivedType(typeof(UpdateServerInfoExtension), (int)Arma3PayLoadTypeExtension.UpdateServerInfo)]
public abstract record Arma3PayloadExtended
{
	public abstract Arma3PayLoadTypeExtension Type { get; }
	public static DateTime Timestamp => DateTime.Now;
	public virtual Task Run(IDiscordBotService service, ServiceDbContext dbContext) => Task.CompletedTask;
}

public record DiscordJsonExtension
(
	DiscordMessageDto DiscordMessage
) : Arma3PayloadExtended
{
	[JsonIgnore]
	public override Arma3PayLoadTypeExtension Type => Arma3PayLoadTypeExtension.DiscordSend;
	private Task<IUserMessage> SendMessage(IDiscordBotService service)
		=> service.SendMessageAsync(DiscordMessage);
	
	public override async Task Run(IDiscordBotService service, ServiceDbContext dbContext)
	{
		await SendMessage(service);
	}
}

public record UpdateServerInfoExtension
(
	string MessageId,
	string TemplateJsonFileName,
	string JsonContent
) : Arma3PayloadExtended
{
	[JsonIgnore]
	public override Arma3PayLoadTypeExtension Type => Arma3PayLoadTypeExtension.UpdateServerInfo;

	public override async Task Run(IDiscordBotService service, ServiceDbContext dbContext)
	{
		var fileInfo = (await CreateTemplate());

		var dbSet = dbContext.UpdateServerInfo;
		var exist = dbSet.FirstOrDefault(o => o.messageId == MessageId);
		
		if (exist == null) {
			await dbSet.AddAsync(new ServerInfo
			{
				messageId = MessageId,
				filePath = fileInfo.FullName,
				createTime = fileInfo.CreationTime,
				
			});
		} else {
			exist.filePath = fileInfo.FullName;
			exist.lastUpdate = DateTime.Now;;
		}
		
		await dbContext.SaveChangesAsync();
	}

	private async Task<FileInfo> CreateTemplate()
	{
		var file = $".profile/InfoTemplate/{TemplateJsonFileName}"; 
		var directory = Path.GetDirectoryName(file);

		if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
		await File.WriteAllTextAsync(file, JsonContent, Encoding.UTF8);

		return new FileInfo(file);
	}
}

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, AllowOutOfOrderMetadataProperties = true)] // Optional: Add desired options
[
	JsonSerializable(typeof(Queue<Arma3PayloadExtended>)),
	JsonSerializable(typeof(Arma3PayloadExtended))
]
public sealed partial class Arma3PayloadExtendedJsonSerializerContext : JsonSerializerContext;
