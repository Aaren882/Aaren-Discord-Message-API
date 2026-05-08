using System.Text.Json.Serialization;
using Discord.WebSocket;

namespace Arma3WebService.Entity.DiscordBotAction;

public enum DiscordBotActionComponentType
{
	Button,
	SelectMenu,
}

public sealed class DiscordBotInteraction: Dictionary<string, DiscordBotActionsBase>
{
	public async Task Execute(SocketMessageComponent component)
	{
		var queried = this
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
public sealed class DiscordBotAdminInteraction: Dictionary<DiscordBotAdminModalType, DiscordBotAdminSimpleAction>
{
	public async Task Execute(SocketMessageComponent component, IEnumerable<string> connectionsNames)
	{
		var values = component.Data.Values ?? [];
		var selectedValue = values.FirstOrDefault(component.Data.CustomId);
		var (type, simpleAction) = this.FirstOrDefault(
			x => string.Equals(x.Key.ToString(), selectedValue, StringComparison.OrdinalIgnoreCase)
		);
		
		simpleAction.ModalType = type;
		simpleAction.ConnectionsNames = connectionsNames;
		await simpleAction.Run(component);
	}
	public async Task Execute(SocketModal modal, IServiceProvider serviceProvider)
	{
		var (type, simpleAction) = this.FirstOrDefault(
			x => 
				string.Equals(x.Key.ToString(), modal.Data.CustomId, StringComparison.OrdinalIgnoreCase)
		);
		
		simpleAction.ModalType = type;
		await simpleAction.Run(modal, serviceProvider);
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
	JsonSerializable(typeof(DiscordBotModalInteraction)),
	JsonSerializable(typeof(DiscordBotAdminInteraction))
]
public sealed partial class DiscordBotActionJsonSerializerContext : JsonSerializerContext;
