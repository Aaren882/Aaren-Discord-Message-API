using System.Collections.Immutable;
using System.Diagnostics.Metrics;
using System.Reflection;
using System.Text.Json;
using Arma3WebService.DBContext;
using Arma3WebService.DBContext.Schema;
using Arma3WebService.Entity.DiscordBotAction;
using Arma3WebService.Models;
using Component.DiscordEntity;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.EntityFrameworkCore;

namespace Arma3WebService.Managers;

public sealed class AdminConsoleManager(
	DiscordSocketClient client,
	InteractionService interactions,
	ILogger<AdminConsoleManager> logger,
	IDiscordBotService discordBotService,
	IServiceProvider serviceProvider,
	IDbContextFactory<ServiceDbContext> dbContextFactory
) : BackgroundService
{
	public static IMessage? AdminMessage { get; private set; }
	public const string MessageFileName = "AdminConsole.json";
	private readonly TimeSpan _ConsoleUpdateTimeSpan = TimeSpan.FromSeconds(10);

	public enum ActionType
	{
		None,
		Modal,
		Button,
		SelectMenu,
	}

	public async Task<DiscordBotAdminInteraction> GetAdminAction(ActionType actionType)
	{
		if (actionType == ActionType.None)
			throw new NotSupportedException("Action type None is not supported.");

		var path = $"AdminConsole/{actionType}Actions.json";
		var json = await File.ReadAllTextAsync(path);
		var deserialize = JsonSerializer.Deserialize(json, DiscordBotActionJsonSerializerContext.Default.DiscordBotAdminInteraction);
		return deserialize ?? throw new JsonException($"Failed to deserialize JSON for action type: {actionType}.");
	}

	public sealed class ComponentModule(
		AdminConsoleManager adminConsoleManager
	) : InteractionModuleBase<SocketInteractionContext>
	{
		[ComponentInteraction("admin_*", runMode: RunMode.Async)]
		public async Task HandleAdminActions()
		{
			DiscordBotAdminInteraction adminAction;
			switch (Context.Interaction)
			{
				case SocketMessageComponent messageComponent:
					AdminConsoleManager.ActionType type = AdminConsoleManager.ActionType.None;
					if (messageComponent.Data.Type == ComponentType.Button)
					{
						type = AdminConsoleManager.ActionType.Button;
					}
					else if (messageComponent.Data.Type == ComponentType.SelectMenu)
					{
						type = AdminConsoleManager.ActionType.SelectMenu;
					}

					adminAction = await adminConsoleManager.GetAdminAction(type);
					await adminAction.Execute(messageComponent, adminConsoleManager);
					break;
				default:
					throw new NullReferenceException("Interaction is not a SocketMessageComponent.");
			}
		}
	}

	public ImmutableArray<string> CreateSessionsNames()
	{
		var names = GetSessionNames();
		return names.Length != 0
			? names
			: throw new Exception("No game session found.");
	}
	public IEnumerable<string> CreateSessionsNames(DiscordBotAdminModalType adminModalType)
	{
		return (adminModalType) switch
		{
			DiscordBotAdminModalType.upload_list => DbProfileNames(),
			_ => CreateSessionsNames()
		};

		ImmutableArray<string> DbProfileNames()
		{
			using var dbContext = dbContextFactory.CreateDbContext();
			var queryable = dbContext.ServerIdentities.Select(x => x.profileName);
			return [.. queryable];
		}
	}

	public async Task<(IMessage, bool)> GetOrAddAdminConsole()
	{
		var channelId = discordBotService.GetPresetMessageChannelId(DiscordBotChannel.AdminConsole);
		var channel = await discordBotService.GetMessageChannelAsync(channelId);
		try
		{
			await using var dbContext = await dbContextFactory.CreateDbContextAsync();
			var exist = dbContext.InternalManagement.FirstOrDefault(
				o => o.managementType == InternalManagementType.AdminConsole);

			IMessage message;
			var isNewMessage = false;
			try
			{
				message = await channel.GetMessageAsync(
					AdminMessage?.Id ??
					exist?.messageId ??
					ulong.MinValue
				) ?? throw new ArgumentException("AdminConsole message not found, proceeding to create new message.");
			}
			catch (ArgumentException ex) //- Catch when GetMessageAsync failed
			{
				logger.LogInformation("It seems there's no AdminConsole message exist. Trying to create a new Console... \n Reason : {Reason}", ex.Message);
				message = await CreateConsole()
					?? throw new NullReferenceException("Admin console message could not be retrieved or created.");
				logger.LogInformation("New AdminConsole is created on \"{Channel}\" : ID - {messageId}", message.Channel.Name, message.Id);

				isNewMessage = true; //- Flag for signaling it's a new message just created !!
			}

			//- Checking DB data
			var updateColumn = true;
			if (exist != null)
			{
				updateColumn = exist.messageId != message.Id;
				if (updateColumn) exist.messageId = message.Id;
			}
			else
			{
				await dbContext.InternalManagement.AddAsync(
					new InternalManagement
					{
						messageId = message.Id,
						description = "it's used for handling remote action on Discord."
					}
				);
			}

			//- Make sure DB updated
			if (updateColumn)
				await dbContext.SaveChangesAsync();

			return (message, isNewMessage);

			//- Local Method
			async Task<IMessage> CreateConsole()
			{
				var json = await File.ReadAllTextAsync(MessageFileName);
				var deserialize = JsonSerializer.Deserialize(
					json,
					MsgPayload_JsonContext.Default.DiscordMessageDto
				);
				return await discordBotService.SendMessageAsync(channelId, deserialize!);
			}
		}
		catch (Exception e)
		{
			logger.LogError(e, "CreateAdminConsole: ");
			await channel.SendMessageAsync($"Exception : {e}");
			throw;
		}
	}

	private ImmutableArray<string> GetSessionNames()
	{
		var webSocketService = serviceProvider.GetRequiredService<IWebSocketService>();
		ImmutableArray<string> names = webSocketService.GetConnectionsNames();

		return names;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		client.ModalSubmitted += async (socketModal) =>
		{
			if (socketModal.Message.Id != AdminMessage?.Id) return; //- Block all non-AdminConsole request

			var adminAction = await GetAdminAction(AdminConsoleManager.ActionType.Modal);
			await adminAction.Execute(socketModal, serviceProvider);
		};

		client.InteractionCreated += async (SocketInteraction) =>
		{
			if (SocketInteraction is not SocketMessageComponent interaction) return;
			try
			{
				//- Block all Not-AdminConsole interaction
				if (interaction.Message != AdminMessage) return;

				SocketInteractionContext context = new(client, interaction);

				var result = await interactions.ExecuteCommandAsync(context, serviceProvider);
				if (!result.IsSuccess)
				{
					logger.LogError("[ERROR] Discord Admin Action : {Reason}\n{ERROR}", result.ErrorReason, result.Error);
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "[EXCEPTION] Discord Admin Action:");
			}
		};
		await interactions.AddModulesAsync(Assembly.GetEntryAssembly(), serviceProvider);

		//- Perf Monitor
		const string rmMeterName = "Microsoft.Extensions.Diagnostics.ResourceMonitoring";

		Dictionary<string, string> samples = [];
		using Meter meter = new(rmMeterName);
		using MeterListener meterListener = new()
		{
			InstrumentPublished = (instrument, listener) =>
			{
				if (instrument.Meter.Name == rmMeterName)
				{
					listener.EnableMeasurementEvents(instrument, null);
				}
			}
		};

		//- Keys can be found from https://learn.microsoft.com/en-us/dotnet/core/diagnostics/built-in-metrics-diagnostics#microsoftextensionsdiagnosticsresourcemonitoring
		meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
		{
			if (instrument.Meter.Name != rmMeterName) return;
			samples[$"{{{instrument.Name}}}"] = $"{measurement:F2}";
		});
		meterListener.Start();

		using PeriodicTimer timer = new(_ConsoleUpdateTimeSpan);
		while (await timer.WaitForNextTickAsync(stoppingToken))
		{
			try
			{
				//- Make suer AdminConsole Exist
				(AdminMessage, bool isNewMessage) = await GetOrAddAdminConsole();

				if (isNewMessage) continue;

				meterListener.RecordObservableInstruments();
				var sessionCount = GetSessionNames().Length;
				var sessionCountColor = sessionCount == 0 ? "arm" : "fix";
				samples["{TOTAL_SESSIONS}"] = @$"{sessionCountColor}\n{sessionCount}";
				samples["{SYSTEM_TIMESTAMP}"] = $"{((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds()}";

				var json = await File.ReadAllTextAsync(MessageFileName, stoppingToken);
				json = samples.Aggregate(
					json,
					(current, item) =>
					{
						var (key, value) = item;
						logger.LogDebug("Admin Panel : {KEY}, {Value}", key, value);
						return current.Replace(key, value, StringComparison.OrdinalIgnoreCase);
					}
				);

				var message = JsonSerializer.Deserialize(
					json,
					MsgPayload_JsonContext.Default.DiscordMessageDto
				);

				await AdminMessage.Channel.ModifyMessageAsync(AdminMessage.Id, msg =>
				{
					msg.Content = message?.Content;
					msg.Embeds = message?.ConvertEmbeds();
					msg.Components = message?.ConvertComponents();
					msg.Flags = message?.Flags;
				});
			}
			catch (OperationCanceledException) { }
			catch (Exception e)
			{
				logger.LogError(e, "\"UpdateConsoleInfo\" throw an Exception.");
			}
		}
	}
}
