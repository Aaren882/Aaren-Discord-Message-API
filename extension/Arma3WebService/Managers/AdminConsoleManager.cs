using System.Text.Json;
using System.Diagnostics.Metrics;
using Arma3WebService.DBContext;
using Arma3WebService.Entity;
using Arma3WebService.Entity.DiscordBotAction;
using Discord;

namespace Arma3WebService.Models;

public sealed class AdminConsoleManager(
	ILogger<AdminConsoleManager> logger,
	IDiscordBotService discordBotService,
	IServiceProvider serviceProvider,
	IServiceScopeFactory serviceScopeFactory
)
{
	internal ulong AdminMessageId;
	
	public enum ActionType
	{
		Modal,
		Button,
		SelectMenu,
	}

	/*public async Task<DiscordBotInteraction?> GetActionJson(ActionType actionType)
	{
		var path = $"AdminConsole/{actionType.ToString()}Actions.json";
		var json = await File.ReadAllTextAsync(path);
		var interaction = JsonSerializer.Deserialize(json, DiscordBotActionJsonSerializerContext.Default.DiscordBotInteraction);
		return (actionType) switch
		{
			ActionType.SelectMenu => await StructureAdminSelectMenu(interaction!),
			_ => interaction
		};
	}

	public async Task<DiscordBotInteraction> StructureAdminSelectMenu(DiscordBotInteraction interaction)
	{
		if (!interaction.TryGetValue("admin_advanced_tools", out var actionsBase))
			throw new NullReferenceException("\"admin_advanced_tools\" doesn't exist in current \"interaction\".");

		const string path = "AdminConsole/SessionSelectMenu.json";
		
		var overwriteJson = await File.ReadAllTextAsync(path);
		var selectMenu = JsonSerializer.Deserialize(
			overwriteJson,
			DiscordBotActionJsonSerializerContext.Default.IDictionaryStringDiscordBotAdminInteractionActions);

		var options = ((DiscordBotAdminSelectMenuActions)actionsBase).OverwriteInteractions(selectMenu);
		
		foreach (var (_, value) in options)
		{
			var labelComponent = new DiscordDto.LabelComponent
			{
				label = "Game Session",
				component = CreateSessionSelectMenuComponent()
			};
			value.InsertComponent(0, labelComponent);
		}
		
		return interaction;
	}*/

	public async Task<DiscordBotAdminInteraction?> GetAdminAction(ActionType actionType)
	{
		var path = $"AdminConsole/{actionType}Actions.json";
		var json = await File.ReadAllTextAsync(path);
		var deserialize = JsonSerializer.Deserialize(json, DiscordBotActionJsonSerializerContext.Default.DiscordBotAdminInteraction);
		return deserialize;
	}

	public List<string> CreateSessionsNames()
	{
		var names = GetSessionNames();
		return names.Count != 0
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

		List<string> DbProfileNames()
		{
			using var scope = serviceScopeFactory.CreateScope();
			using var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
			var queryable = dbContext.ServerIdentities.Select(x => x.profileName);
			return queryable.ToList();
		}
	}
	
	public async Task CreateAdminConsole()
    {
    	var channelId = discordBotService.GetPresetMessageChannelId(DiscordBotChannel.AdminConsole);
    	var channel = await discordBotService.GetMessageChannelAsync(channelId);
    	try
    	{
    		using var scope = serviceScopeFactory.CreateScope();
    		await using var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
    		var exist = dbContext.InternalManagement.FirstOrDefault(
    			o => 
    				o.managementType == InternalManagementType.AdminConsole
    			);

    		var updateColumn = true;
    		IMessage message;
    		
    		//- Checking DB data
    		if (exist is null)
		    {
			    message = await CreateConsole();

    			await dbContext.InternalManagement.AddAsync(
    				new InternalManagement
    				{
    					messageId = message.Id,
    					description = "it's used for handling remote action on Discord."
    				}
    			);
    		} else
    		{
			    message = await channel.GetMessageAsync(exist.messageId);
				var id = message?.Id ?? 0;
				if (id == 0)
					message = await CreateConsole();
    			
				updateColumn = exist.messageId != id;
    			if (updateColumn) exist.messageId = message!.Id;
    		}

		    AdminMessageId = message!.Id;
		    _ = UpdateConsoleInfo(channel, AdminMessageId);
    		
    		//- Make sure DB updated
    		if (updateColumn)
    			await dbContext.SaveChangesAsync();
			
		    //- Local Method
		    async Task<IMessage> CreateConsole()
		    {
			    var json = await File.ReadAllTextAsync("AdminConsole.json");
			    var deserialize = JsonSerializer.Deserialize(
				    json,
				    MsgPayload_JsonContext.Default.DiscordMessageDto
			    );
			    return await discordBotService.SendMessageAsync(channelId, deserialize!);
		    };
	    }
    	catch (Exception e)
    	{
    		logger.LogError("ERROR CreateAdminConsole : {Error}", e.Message);
    		await channel.SendMessageAsync($"Exception : {e.Message}");
    	}
    }

	private List<string> GetSessionNames()
	{
		var webSocketService = serviceProvider.GetRequiredService<IWebSocketService>();
		var names = webSocketService.GetConnectionsNames().ToList();
		// List<string> names = ["FeatureTest","test2"];

		return names;
	}
	
	private async Task UpdateConsoleInfo(IMessageChannel channel, ulong messageID)
	{
		var samples = new Dictionary<string, string>();
		const string rmMeterName = "Microsoft.Extensions.Diagnostics.ResourceMonitoring";
		using var meter = new Meter(rmMeterName);
		using var meterListener = new MeterListener
		{
			InstrumentPublished = (instrument, listener) =>
			{
				if (instrument.Meter.Name == rmMeterName &&
				    instrument.Name.StartsWith("container."))
				{
					listener.EnableMeasurementEvents(instrument, null);
				}
			}
		};
    
		meterListener.SetMeasurementEventCallback<double>((instrument, measurement, tags, state) =>
		{
			if (instrument.Meter.Name != rmMeterName) return;
			samples[$"{{{instrument.Name}}}"] = $"{measurement:F2}";
		});
		meterListener.Start();

		while (true)
		{
			try
			{
				meterListener.RecordObservableInstruments();
				var sessionCount = GetSessionNames().Count;
				var sessionCountColor = sessionCount == 0 ? "arm" : "fix";
				samples["{TOTAL_SESSIONS}"] = @$"{sessionCountColor}\n{sessionCount}";
				samples["{SYSTEM_TIME}"] = $"{((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds()}";
				
				var json = await File.ReadAllTextAsync("AdminConsole.json");
				json = samples.Aggregate(
					json,
					(current, item) => current.Replace(item.Key, item.Value, StringComparison.OrdinalIgnoreCase)
				);
				
				var message = JsonSerializer.Deserialize(
					json,
					MsgPayload_JsonContext.Default.DiscordMessageDto
				);

				await channel.ModifyMessageAsync(messageID, msg =>
				{
					msg.Content = message.Content;
					msg.Embeds = message.ConvertEmbeds();
					msg.Components = message.ConvertComponents();
					msg.Flags = message.Flags;
				});
				
				await Task.Delay(10000);
			} catch (Exception e)
			{
				logger.LogError(e, "\"UpdateConsoleInfo\" throw an Exception.");
			}
		}
	}
}
