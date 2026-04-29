using System.Text.Json.Serialization;
using Discord;
using Discord.WebSocket;

namespace Arma3WebService.Entity.DiscordBotAction;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DiscordBotSelectMenuRespond), nameof(DiscordBotButtonActionType.Respond))]
public abstract record DiscordBotSelectMenu : DiscordBotActionBase
{
	public override Task Run(SocketMessageComponent component) => Task.CompletedTask;
}

public record DiscordBotSelectMenuRespond(
	DiscordMessageDto message,
	bool? ephemeral,
	AllowedMentions? allowedMentions,
	RequestOptions? options
): DiscordBotSelectMenu
{
	public override async Task Run(SocketMessageComponent component)
	{
		var embed = message.ConvertEmbeds();
		var components = message.ConvertComponents();
		var pollProperties = message.ConvertPolls();
		var selectedValue = component.Data.Values.First();
		
		await component.RespondAsync(
			text: $"You selected: {selectedValue}",
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
