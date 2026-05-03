using System.Text.Json.Serialization;
using Discord;
using Discord.WebSocket;

namespace Arma3WebService.Entity.DiscordBotAction;

public enum DiscordBotButtonActionType
{
	Respond,
	WithFile,
	RespondModal,
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DiscordBotButtonRespond), nameof(DiscordBotButtonActionType.Respond))]
[JsonDerivedType(typeof(DiscordBotButtonWithFile), nameof(DiscordBotButtonActionType.WithFile))]
[JsonDerivedType(typeof(DiscordBotButtonModalRespond), nameof(DiscordBotButtonActionType.RespondModal))]
public abstract record DiscordBotSimpleAction : DiscordBotActionBase
{
	public override Task Run(SocketMessageComponent component) => Task.CompletedTask;
}

public record DiscordBotButtonRespond(
	DiscordMessageDto message,
	bool? ephemeral,
	AllowedMentions? allowedMentions,
	RequestOptions? options
): DiscordBotSimpleAction
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

public record DiscordBotButtonWithFile(
	DiscordMessageDto message,
	bool? ephemeral,
	AllowedMentions? allowedMentions,
	RequestOptions? options
): DiscordBotSimpleAction
{
	public override async Task Run(SocketMessageComponent component) {
		if (message.File is null)
		{
			await component.RespondAsync(text: "Please specify a file.", ephemeral: true);
			return;
		}
		try
		{
			var stream = File.OpenRead(Path.GetFullPath(message.File));
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
		catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
		{
			await component.RespondAsync(text: $"File exception : {e.Message}", ephemeral: true);
		}
	}
}

public record DiscordBotButtonModalRespond(
	DiscordDto.ModalComponent message,
	RequestOptions? options
): DiscordBotSimpleAction
{
	public override async Task Run(SocketMessageComponent component)
	{
		var modal = message.Build();
		await component.RespondWithModalAsync(modal, options);
	}
}
