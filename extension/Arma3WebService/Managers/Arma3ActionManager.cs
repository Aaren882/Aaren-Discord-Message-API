using Components.Entity;
using Arma3WebService.Models;

namespace Arma3WebService.Managers;

public interface IArma3ActionManager
{
	// Task GetAction(Arma3Action action);
	Task GetAction(IConnection connection, Arma3Payload payload);
}

public sealed class Arma3ActionManager(ServiceActionManager serviceAction, IDiscordBotService discordBotService) : IArma3ActionManager
{
	public async Task GetAction(IConnection connection, Arma3Payload payload)
	{
		// var (connection, payload) = action;

		try
		{
			var result = payload switch
			{
				Arma3PayloadText payloadText => 
					serviceAction.TextAction(connection, payloadText),
				Arma3PayloadBinary payloadBinary =>
					serviceAction.BinaryAction(connection, payloadBinary),
				Arma3PayloadCallBack payloadCallBack =>
					serviceAction.CallBackAction(connection, payloadCallBack),
				Arma3PayloadServiceRequest payloadServiceRequest => 
					serviceAction.ServiceRequestAction(connection, payloadServiceRequest),
				Arma3PayloadJson payloadJson =>
					serviceAction.JsonStringAction(connection, payloadJson),
				Arma3PayloadFlatJsonString payloadFlatJsonString => 
					serviceAction.FlatJsonStringAction(connection, payloadFlatJsonString),
				
				_ => throw new ArgumentOutOfRangeException(nameof(payload.Type), payload.Type, null)
			};
			await result;
		}
		catch (Exception e)
		{
			var id = discordBotService.GetPresetMessageChannelId(DiscordBotChannel.Logging);
			var channel = await discordBotService.GetMessageChannelAsync(id);
			await channel.SendMessageAsync($"```diff\n- {e.Message}\n```");
		}
	}
}
