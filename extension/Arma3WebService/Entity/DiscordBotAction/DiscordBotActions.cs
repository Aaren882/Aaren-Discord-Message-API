using System.Text.Json.Serialization;
using Discord.WebSocket;

namespace Arma3WebService.Entity.DiscordBotAction;

public enum DiscordBotActionComponentType
{
	Button,
}
public enum DiscordBotActionType
{
	Respond,
	SendFile
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
		
		foreach (var DiscordBotAction in queried)
			await DiscordBotAction.Execute(component);
	}
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "componentType")]
[JsonDerivedType(typeof(DiscordBotButtonActions), nameof(DiscordBotActionComponentType.Button))]
public abstract record DiscordBotActionsBase
{
	public IEnumerable<DiscordBotActionBase> Steps { get; set; }
	public abstract Task Execute(SocketMessageComponent component);
}

public record DiscordBotButtonActions : DiscordBotActionsBase
{
	public IEnumerable<DiscordBotButton> Steps { get; set; }
	public override async Task Execute(SocketMessageComponent component)
	{
		foreach (var discordBotAction in Steps)
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
[JsonSerializable(typeof(DiscordBotInteraction))]
public sealed partial class DiscordBotActionJsonSerializerContext : JsonSerializerContext;
