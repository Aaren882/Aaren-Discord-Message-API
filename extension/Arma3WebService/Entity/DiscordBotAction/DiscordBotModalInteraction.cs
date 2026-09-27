using Discord.WebSocket;

namespace Arma3WebService.Entity.DiscordBotAction;

public sealed class DiscordBotModalInteraction: Dictionary<string, DiscordBotModalActions>
{
	public async Task Execute(SocketModal modal)
	{
		var results = this.Join(
			modal.Data.Components,
			action => action.Key,
			component => component.CustomId,
			(action, component) => action.Value.Execute(modal, component)
		).ToList();
		
		foreach (var result in results)
			await result;
	}
}

public record DiscordBotModalActions
{
	public IEnumerable<DiscordBotModal> Steps { get; set; }
	public async Task Execute(SocketModal modal, SocketMessageComponentData component)
	{
		foreach (var discordBotAction in Steps)
			await discordBotAction.Run(modal, component);
	}
}
