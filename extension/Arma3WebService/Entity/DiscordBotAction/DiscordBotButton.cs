using System.Text.Json.Serialization;
using Discord;
using Discord.WebSocket;
using ModalBuilder = Discord.Interactions.Builders.ModalBuilder;

namespace Arma3WebService.Entity.DiscordBotAction;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DiscordBotRespond), nameof(DiscordBotActionType.Respond))]
[JsonDerivedType(typeof(DiscordBotSendFile), nameof(DiscordBotActionType.SendFile))]
[JsonDerivedType(typeof(DiscordBotModalRespond), nameof(DiscordBotActionType.RespondModal))]
public abstract record DiscordBotButton : DiscordBotActionBase
{
	public override Task Run(SocketMessageComponent component) => Task.CompletedTask;
}

public record DiscordBotRespond(
	DiscordMessageDto message,
	bool? ephemeral,
	bool? isV2,
	AllowedMentions? allowedMentions,
	RequestOptions? options
): DiscordBotButton
{
	public override async Task Run(SocketMessageComponent component)
	{
		var embed = message.ConvertEmbeds();
		var components = message.ConvertComponents();
		var pollProperties = message.ConvertPolls();
		
		await component.RespondAsync(
			text: message.Content,
			isTTS : message.Tts ?? false,
			ephemeral: ephemeral ?? false,
			allowedMentions: allowedMentions,
			components: components,
			embeds: embed,
			options: options,
			poll: pollProperties,
			flags: message.Flags
		);
	}
}

public record DiscordBotSendFile(
	DiscordMessageDto message,
	bool? ephemeral,
	AllowedMentions? allowedMentions,
	RequestOptions? options
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
			// poll: poll,
			flags: message.Flags
		);
	}
}

public record DiscordBotModalRespond(
	DiscordDto.ModalComponent message,
	RequestOptions? options
): DiscordBotButton
{
	public override async Task Run(SocketMessageComponent component)
	{
		var modal = message.Build();
		await component.RespondWithModalAsync(modal, options);
	}
}
