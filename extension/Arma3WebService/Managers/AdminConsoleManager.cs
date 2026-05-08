using System.Text.Json;
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
	private readonly ulong _adminChannel = ulong.Parse(Environment.GetEnvironmentVariable("AdminChannel")!);
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

	public IEnumerable<string> CreateSessionsNames()
	{
		var webSocketService = serviceProvider.GetRequiredService<IWebSocketService>();

		// var names = webSocketService.GetConnectionsName().ToList();
		List<string> names = ["FeatureTest","test2"];

		return names.Count != 0
			? names
			: throw new Exception("No game session found.");
	}
	
	public async Task CreateAdminConsole()
    {
    	var channel = await discordBotService.GetMessageChannelAsync(_adminChannel);
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
    			var json = await File.ReadAllTextAsync("AdminConsole.json");
    			var deserialize = JsonSerializer.Deserialize(
    				json,
    				MsgPayload_JsonContext.Default.DiscordMessageDto
    			);
    			message = await discordBotService.SendMessageAsync(_adminChannel, deserialize!);

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
    			updateColumn = exist.messageId != message.Id;
    			
    			if (updateColumn) exist.messageId = message.Id;
    		}

		    AdminMessageId = message.Id;
    		
    		//- Make sure DB updated
    		if (updateColumn)
    			await dbContext.SaveChangesAsync();
    	}
    	catch (Exception e)
    	{
    		logger.LogError("ERROR CreateAdminConsole : {Error}", e.Message);
    		await channel.SendMessageAsync($"Exception : {e.Message}");
    	}
    }
}
