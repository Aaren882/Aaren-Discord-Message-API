using System.Threading.Channels;
using Arma3WebService.Models;
using Components.Entity;

namespace Arma3WebService.Managers;

public interface IArma3ActionManager
{
	public CancellationTokenSource Cts { get; init; }
	bool TryEnqueueAction(WebsocketServer connection, Arma3Payload payload);
}

public sealed class Arma3ActionManager : IArma3ActionManager
{
	private readonly struct ActionPayload(WebsocketServer connection, Arma3Payload payload)
	{
		public void Deconstruct(out WebsocketServer Connection, out Arma3Payload Payload)
		{
			Connection = connection;
			Payload = payload;
		}
	};
	private readonly Channel<ActionPayload> _ActionChannel = Channel.CreateBounded<ActionPayload>(1000);
	public CancellationTokenSource Cts { get; init; }
	private readonly ServiceActionManager ServiceAction;
	private readonly IDiscordBotService DiscordBotService;
	private readonly Task _mainLoop;
	public Arma3ActionManager(ServiceActionManager serviceAction, IDiscordBotService discordBotService)
	{
		ServiceAction = serviceAction;
		DiscordBotService = discordBotService;
		_mainLoop = DoAction();
		Cts = new();
	}

	public bool TryEnqueueAction(WebsocketServer connection, Arma3Payload payload)
	{
		return _ActionChannel.Writer.TryWrite(new(connection, payload));
	}

	private async ValueTask GetAction(ActionPayload action)
	{
		var (connection, payload) = action;
		try
		{
			var result = payload switch
			{
				Arma3PayloadText payloadText =>
					ServiceAction.TextAction(connection, payloadText),
				Arma3PayloadBinary payloadBinary =>
					ServiceAction.BinaryAction(connection, payloadBinary),
				Arma3PayloadBinaryContent payloadBinary =>
					ServiceAction.BinaryContentAction(connection, payloadBinary),
				Arma3PayloadCallBack payloadCallBack =>
					ServiceAction.CallBackAction(connection, payloadCallBack),
				Arma3PayloadServiceRequest payloadServiceRequest =>
					ServiceAction.ServiceRequestAction(connection, payloadServiceRequest),
				Arma3PayloadJson payloadJson =>
					ServiceAction.JsonStringAction(connection, payloadJson),
				Arma3PayloadFlatJsonString payloadFlatJsonString =>
					ServiceAction.FlatJsonStringAction(connection, payloadFlatJsonString),

				_ => throw new ArgumentOutOfRangeException(nameof(payload.Type), payload.Type, null)
			};
			await result;
		}
		catch (Exception e)
		{
			var id = DiscordBotService.GetPresetMessageChannelId(DiscordBotChannel.Logging);
			var channel = await DiscordBotService.GetMessageChannelAsync(id);
			await channel.SendMessageAsync($"```diff\n- {e.Message}\n```");
		}
	}
	private async Task DoAction()
	{
		while (await _ActionChannel.Reader.WaitToReadAsync())
		{
			if (_ActionChannel.Reader.TryRead(out var action))
				await GetAction(action);
		}
	}
}
