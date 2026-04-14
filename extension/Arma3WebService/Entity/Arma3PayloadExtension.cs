using System.Text;
using System.Text.Json.Serialization;
using Arma3WebService.Models;

namespace Arma3WebService.Entity;

public enum Arma3PayLoadTypeExtension
{
	DiscordSend = 1,
	UpdateServerInfo = 2,
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "ProcessType")]
[JsonDerivedType(typeof(DiscordJsonExtension), (int)Arma3PayLoadTypeExtension.DiscordSend)]
[JsonDerivedType(typeof(UpdateServerInfoExtension), (int)Arma3PayLoadTypeExtension.UpdateServerInfo)]
public abstract record Arma3PayloadExtension
{
	public abstract Arma3PayLoadTypeExtension Type { get; }
	public static DateTime Timestamp => DateTime.Now;
}

public record DiscordJsonExtension
(
	DiscordMessageDto DiscordMessage
) : Arma3PayloadExtension
{
	[JsonIgnore]
	public override Arma3PayLoadTypeExtension Type => Arma3PayLoadTypeExtension.DiscordSend;

	public async Task SendMessage(IDiscordBotService service)
	{
		await service.SendMessageAsync(DiscordMessage);
	}
};

public record UpdateServerInfoExtension
(
	string MessageId,
	string TemplateJsonFileName,
	string JsonContent
) : Arma3PayloadExtension
{
	[JsonIgnore]
	public override Arma3PayLoadTypeExtension Type => Arma3PayLoadTypeExtension.UpdateServerInfo;

	public async Task CreateTemplate()
	{
		var file = $".profile/InfoTemplate/{TemplateJsonFileName}.json"; 
		var directory = Path.GetDirectoryName(file);

		if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);
		await File.WriteAllTextAsync(file, JsonContent, Encoding.UTF8);
	}
};

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, AllowOutOfOrderMetadataProperties = true)] // Optional: Add desired options
[
	JsonSerializable(typeof(Arma3PayloadExtension))
]
public sealed partial class Arma3PayloadExtensionJsonSerializerContext : JsonSerializerContext;
