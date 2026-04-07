using Arma3WebService.Models;
using Components.Entity;

namespace Arma3WebService.Entity;

public sealed class ServiceAction(
	IDiscordBotService discordBotService
)
{
	public async Task InvokeArmaCallBack(IConnection session, Arma3PayloadCallBack command)
	{
		await session.SendArmaCallback(command);
	}
		
	public async Task InvokeDiscordBotMessage(DiscordMessageDto message)
	{
		await discordBotService?.SendMessageAsync(message)!;
	}
}
