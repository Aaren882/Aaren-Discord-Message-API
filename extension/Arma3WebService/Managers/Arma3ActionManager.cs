using System.Threading.Channels;
using Arma3WebService.Models;
using Components.Entity;
using static Arma3WebService.Managers.WebsocketServer;

namespace Arma3WebService.Managers;

public interface IArma3ActionManager
{
	bool TryEnqueueAction(WebsocketServer connection, Arma3Payload payload);
}

public sealed class Arma3ActionManager(
	ILogger<Arma3ActionManager> Logger,
	ServiceActionManager ServiceAction,
	IDiscordBotService DiscordBotService,
	Channel<ActionPayload> _ActionChannel
) : BackgroundService, IArma3ActionManager
{
	/* private ILogger<Arma3ActionManager> Logger { get; init; }
	private Task _mainLoop;
	public readonly CancellationTokenSource Cts = new();
	private readonly ServiceActionManager ServiceAction;
	private readonly IDiscordBotService DiscordBotService;
	public Arma3ActionManager(
		ILogger<Arma3ActionManager> logger,
		ServiceActionManager serviceAction,
		IDiscordBotService discordBotService
	)
	{
		Logger = logger;
		ServiceAction = serviceAction;
		DiscordBotService = discordBotService;
		_mainLoop = DoAction(Cts.Token);
	} */
	// public readonly Channel<ActionPayload> _ActionChannel = Channel.CreateBounded<ActionPayload>(1000);
	public bool TryEnqueueAction(WebsocketServer connection, Arma3Payload payload)
	{
		Logger.LogTrace("[Writer] Start writing Channel. Channel Hash: {Hash}", _ActionChannel.GetHashCode());
		var success = _ActionChannel.Writer.TryWrite(new(connection, payload));
		Logger.LogTrace("[Writer] TryWrite Result: {Success}。Item Counts: {Count}", success, _ActionChannel.Reader.Count);
		return success;
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
				Arma3PayloadCallBack payloadCallBack =>
					ServiceAction.CallBackAction(connection, payloadCallBack),
				Arma3PayloadUpdateDB payloadUpdateDB =>
					ServiceAction.UpdateDBAction(connection, payloadUpdateDB),
				Arma3PayloadServiceRequest payloadServiceRequest =>
					ServiceAction.ServiceRequestAction(connection, payloadServiceRequest),
				Arma3PayloadJson payloadJson =>
					ServiceAction.JsonStringAction(connection, payloadJson),
				Arma3PayloadFlatJsonString payloadFlatJsonString =>
					ServiceAction.FlatJsonStringAction(connection, payloadFlatJsonString),

				_ => throw new NotSupportedException($"Unsupported payload type encountered: {payload.GetType().Name}")
			};
			await result;
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "An error occurred while processing the action.");
			var id = DiscordBotService.GetPresetMessageChannelId(DiscordBotChannel.Logging);
			var channel = await DiscordBotService.GetMessageChannelAsync(id);
			await channel.SendMessageAsync($"```diff\n- {ex.Message}\n```");
		}
	}
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		Logger.LogInformation("{Service} service started. HashCode : {HashCode}, Thread : {ThreadID}", nameof(Arma3ActionManager), _ActionChannel.GetHashCode(), Environment.CurrentManagedThreadId);
		try
		{
			await foreach (var action in _ActionChannel.Reader.ReadAllAsync(stoppingToken))
			{
				await GetAction(action);
			}
		}
		catch (OperationCanceledException) { }
		catch (Exception ex)
		{
			Logger.LogError(ex, "An error occurred during binary stream processing.");
		}
		finally
		{
			Logger.LogCritical("Binary stream processing loop terminated.");
		}
	}
	/* private async Task DoAction(CancellationToken stoppingToken)
	{
		Logger.LogInformation("{Service} service started. HashCode : {HashCode}, Thread : {ThreadID}", nameof(Arma3ActionManager), _ActionChannel.GetHashCode(), Environment.CurrentManagedThreadId);
		try
		{
			await foreach (var action in _ActionChannel.Reader.ReadAllAsync(stoppingToken))
			{
				await GetAction(action);
			}
		}
		catch (OperationCanceledException) { }
		catch (Exception ex)
		{
			Logger.LogError(ex, "An error occurred during binary stream processing.");
		}
		finally
		{
			Logger.LogCritical("Binary stream processing loop terminated.");
		}
	} */
}
