using System.Collections.Concurrent;
using System.Net.Mime;
using Arma3WebService.DBContext;
using Arma3WebService.Entity.DiscordBotAction;
using Arma3WebService.Models;
using Components.Entity;
using Discord.WebSocket;

namespace Arma3WebService.Handler;

using RespondHelper = DiscordBotAdminModalRespondHelper;
internal static class DiscordBotAdminSubmitHelper
{
	private delegate Task SubmitAction(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider);

	public static async Task Extension(this DiscordBotAdminSimpleAction simpleAction, SocketModal component,
		IServiceProvider serviceProvider)
	{
		SubmitAction content = (simpleAction.ModalType) switch
		{
			DiscordBotAdminModalType.upload_list => UploadList,
			DiscordBotAdminModalType.print_log => PrintLog,
			DiscordBotAdminModalType.export_log => ExportLog,
			DiscordBotAdminModalType.admin_restart_mission => AdminRestartMission,
			DiscordBotAdminModalType.admin_broadcast => AdminBroadcast,
			_ => throw new ArgumentOutOfRangeException(nameof(simpleAction), "\"ModalType\" does not exist in the options.")
		};
		await content(component, simpleAction, serviceProvider);
	}
	private static async Task UploadList(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		var attachments = component.Data.Attachments.ToList();
		var attachment = attachments[0];
		if (!attachment.ContentType.Contains(MediaTypeNames.Text.Html)) throw new Exception("Invalid content type.");
		
		//- Saving Url
		var serviceScopeFactory= serviceProvider.GetRequiredService<IServiceScopeFactory>();
		await using var scoped = serviceScopeFactory.CreateAsyncScope();
		await using var dbContext = scoped.ServiceProvider.GetRequiredService<ServiceDbContext>();
		
		//- Get correct server info
		var sessionName = GetSelectedSession(component);
		var serverIdentity= await dbContext.GetServerIdentityFromProfileNameAsync(sessionName);
		if (serverIdentity is null)
			throw new NullReferenceException($"\"serverIdentity : {serverIdentity}\" is not exist.");
		
		var infoTemplate = dbContext.ServerInfoList
			.FirstOrDefault(o => o.messageId == serverIdentity.messageId);

		if (infoTemplate == null) throw new NullReferenceException("\"infoTemplate\" does not exist.");

		//https://cdn.discordapp.com/ephemeral-attachments/1502234846066376807/1502236089145098350/Arma_3_Preset_tfox_greensea.html?ex=69fef9e1&is=69fda861&hm=645634eced13e56caf4cf41039a5f735542f2efacc9c3d40e540cb8152214ac0&
		var url = attachment.Url;
		infoTemplate.modListUrl = url;
		await dbContext.SaveChangesAsync();

		//- Respond with a message
		await component.RespondAsync($"`Mod List update Completed !` \n\n {url}", ephemeral: true);
	}
	
	internal static ConcurrentDictionary<string, SocketModal> SubmittedPrintLogModalSockets = new();
	private static async Task PrintLog(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		var webSocketService = serviceProvider.GetRequiredService<IWebSocketService>();
		var session = GetSelectedSession(component);
		var connection = webSocketService.GetConnection(session);

		//component.GuildId
		var guildId = $"{component.GuildId}";
		/*if (!SubmittedPrintLogModalSockets.TryAdd(guildId, component))
		{
			await component.RespondAsync("Your request is already submitted (Still waiting for a response).");
			return;
		}*/
		SubmittedPrintLogModalSockets[guildId] = component;
		var command = new Arma3PayloadRptLine("", component.CreatedAt.DateTime, guildId);
		await connection.SendArmaCallBackMessage(command);
	}
	private static async Task ExportLog(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		
	}
	private static async Task AdminRestartMission(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		
	}
	private static async Task AdminBroadcast(SocketModal component, DiscordBotAdminSimpleAction simpleAction, IServiceProvider serviceProvider)
	{
		//- Saving Url
		var webSocketService= serviceProvider.GetRequiredService<IWebSocketService>();
		var serviceScopeFactory= serviceProvider.GetRequiredService<IServiceScopeFactory>();
		await using var scoped = serviceScopeFactory.CreateAsyncScope();
		await using var dbContext = scoped.ServiceProvider.GetRequiredService<ServiceDbContext>();
		
		//- Get correct server info
		var sessionName = GetSelectedSession(component);
		var serverIdentity= await dbContext.GetServerIdentityFromProfileNameAsync(sessionName);
		if (serverIdentity is null)
			throw new NullReferenceException($"\"serverIdentity : {serverIdentity}\" is not exist.");

		var componentCustomId = simpleAction.ModalType.GetComponentCustomId().First();
		var inputComponent = component.Data.Components.First(x => string.Equals(x.CustomId, componentCustomId, StringComparison.OrdinalIgnoreCase));
		
		var remoteCommand = new Arma3RemoteCommand
		{
			gameId = serverIdentity.profileName,
			payload = new Arma3PayloadCallBack(
				nameof(AdminBroadcast),
				$"[\"{inputComponent.Value}\"]"
			)
		};
		await webSocketService.InvokeArmaCallBack(remoteCommand);
		await component.RespondAsync($"`\"{nameof(AdminBroadcast)}\" Completed !`", ephemeral: true);
	}

	private static string GetSelectedSession(SocketModal component)
	{
		var sessionSelectMenu = component.Data.Components.FirstOrDefault(o => 
			o.CustomId == RespondHelper.SessionSelectMenuComponentCustomId);
		
		return sessionSelectMenu is null
			? throw new Exception("Cannot Find Session Select Menu")
			: sessionSelectMenu.Values.First();
	}
	
}
