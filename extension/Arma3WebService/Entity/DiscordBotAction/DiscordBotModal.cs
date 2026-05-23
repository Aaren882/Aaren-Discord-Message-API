using System.Text.Json.Serialization;
using Discord.WebSocket;

namespace Arma3WebService.Entity.DiscordBotAction;

public enum DiscordBotModalActionType
{
	GameInteraction
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(DiscordBotModalInGameInteraction), nameof(DiscordBotModalActionType.GameInteraction))]
public abstract record DiscordBotModal : DiscordBotActionBase
{
	public virtual Task Run(SocketModal modal, SocketMessageComponentData componentData) => Task.CompletedTask;
}

public record DiscordBotModalInGameInteraction: DiscordBotModal
{
	public override async Task Run(SocketModal modal, SocketMessageComponentData componentData)
	{
		var messageId = modal.Message.Id;
		Console.WriteLine(messageId); //- messageId is identical (Trace back to the game session) 
	}
}
