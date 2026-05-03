using System.Text.Json.Serialization;
using Discord;
using Discord.WebSocket;

namespace Arma3WebService.Entity.DiscordBotAction;

public enum DiscordBotActionComponentType
{
	Button,
	SelectMenu,
	Modal,
}

public class DiscordBotInteraction
{
	public IDictionary<string, DiscordBotActionsBase> Actions { get; set; }

	public async Task Execute(SocketMessageComponent component)
	{
		var queried = Actions
			.Where(
				x => x.Key == component.Data.CustomId
			)
			.Select(
				x => x.Value
			);
		
		foreach (var discordBotAction in queried)
			await discordBotAction.Execute(component);
	}
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "componentType")]
[JsonDerivedType(typeof(DiscordBotInteractionActions), nameof(DiscordBotActionComponentType.Button))]
[JsonDerivedType(typeof(DiscordBotSelectMenuActions), nameof(DiscordBotActionComponentType.SelectMenu))]
public abstract record DiscordBotActionsBase
{
	public IEnumerable<DiscordBotActionBase> Steps { get; set; }
	public abstract Task Execute(SocketMessageComponent component);
}

public record DiscordBotInteractionActions : DiscordBotActionsBase
{
	public IEnumerable<DiscordBotSimpleAction> Steps { get; set; }
	public override async Task Execute(SocketMessageComponent component)
	{
		foreach (var discordBotAction in Steps)
			await discordBotAction.Run(component);
	}
}
public record DiscordBotSelectMenuActions : DiscordBotActionsBase
{
	public IDictionary<string, DiscordBotInteractionActions> Options { get; set; }
	public override async Task Execute(SocketMessageComponent component)
	{
		var selectedValue = component.Data.Values.First();
		var action = Options[selectedValue];
		
		foreach (var discordBotAction in action.Steps.ToList())
			await discordBotAction.Run(component);
	}
}

public record DiscordBotActionBase
{
	public virtual Task Run(SocketMessageComponent component) => Task.CompletedTask;
}

[JsonSourceGenerationOptions(
	PropertyNameCaseInsensitive = true,
	AllowOutOfOrderMetadataProperties = true
)]
[
	JsonSerializable(typeof(DiscordBotInteraction)),
	JsonSerializable(typeof(DiscordBotModalInteraction))
]
public sealed partial class DiscordBotActionJsonSerializerContext : JsonSerializerContext;
