using System.Text.Json;
using Arma3WebService.DBContext;
using Arma3WebService.Entity;
using Discord;

namespace Arma3WebService.Models;

public sealed class AdminConsoleManager(
	ILogger<AdminConsoleManager> logger,
	IServiceProvider serviceProvider,
	IServiceScopeFactory serviceScopeFactory
)
{
	private readonly IDiscordBotService _discordBotService = serviceProvider.GetRequiredService<IDiscordBotService>();
	private readonly ulong _adminChannel = ulong.Parse(Environment.GetEnvironmentVariable("AdminChannel")!);
	internal ulong AdminMessageId;
	
	public enum ActionType
	{
		Modal,
		Button,
		SelectMenu,
	}

	public async Task<string> GetActionJson(ActionType actionType)
	{
		var path = $"AdminConsole/{actionType.ToString()}Actions.json";
		return await File.ReadAllTextAsync(path);
	}
	public async Task CreateAdminConsole()
    {
    	var channel = await _discordBotService.GetMessageChannelAsync(_adminChannel);
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
    			message = await _discordBotService.SendMessageAsync(_adminChannel, deserialize!);

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
