using System.Text.Json.Serialization;
using Discord;
using Discord.WebSocket;

namespace Arma3WebService.Entity.DiscordBotAction;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DiscordBotRespond), nameof(DiscordBotActionType.Respond))]
[JsonDerivedType(typeof(DiscordBotSendFile), nameof(DiscordBotActionType.SendFile))]
public abstract record DiscordBotButton : DiscordBotActionBase
{
	public new virtual Task Run(SocketMessageComponent component) => Task.CompletedTask;
}

public record DiscordBotRespond(
	DiscordMessageDto message,
	bool? ephemeral,
	AllowedMentions? allowedMentions,
	RequestOptions? options,
	PollProperties? poll
): DiscordBotButton
{
	public override async Task Run(SocketMessageComponent component)
	{
		var embed = message.ConvertEmbeds();
		var components = message.ConvertComponents();
		
		await component.RespondAsync(
			text: message.Content,
			isTTS : message.Tts ?? false,
			ephemeral: ephemeral ?? false,
			allowedMentions: allowedMentions,
			components: components,
			embeds: embed,
			options: options,
			poll: poll,
			flags: message.Flags ?? MessageFlags.None
		);
	}
}

public record DiscordBotSendFile(
	DiscordMessageDto message,
	bool? ephemeral,
	AllowedMentions? allowedMentions,
	RequestOptions? options,
	PollProperties? poll
): DiscordBotButton
{
	public override async Task Run(SocketMessageComponent component) {
		var stream = File.OpenRead(Path.GetFullPath(message.File!));
		var embed = message.ConvertEmbeds();
		var components = message.ConvertComponents();
		
		await component.RespondWithFileAsync(
			fileStream: stream,
			fileName: message.FileName,
			text: message.Content,
			isTTS : message.Tts ?? false,
			ephemeral: ephemeral ?? false,
			allowedMentions: allowedMentions,
			components: components,
			embeds: embed,
			options: options,
			poll: poll,
			flags: message.Flags ?? MessageFlags.None
		);
	}
};
