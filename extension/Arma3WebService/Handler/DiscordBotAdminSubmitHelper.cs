using System.Net.Mime;
using Arma3WebService.DBContext;
using Arma3WebService.Entity.DiscordBotAction;
using Discord.WebSocket;

namespace Arma3WebService.Handler;

internal static class DiscordBotAdminSubmitHelper
{
	private delegate Task SubmitAction(SocketModal component, IServiceProvider serviceProvider);

	public static async Task Extension(this DiscordBotAdminSimpleAction simpleAction, SocketModal component,
		IServiceProvider serviceProvider)
	{
		SubmitAction content = (simpleAction.ModalType) switch
		{
			DiscordBotAdminModalType.upload_list => UploadList,
			// DiscordBotAdminModalType.print_log => PrintLog,
			// DiscordBotAdminModalType.export_log => ExportLog,
			_ => throw new ArgumentOutOfRangeException(nameof(simpleAction), "\"ModalType\" does not exist in the options.")
		};
		await content(component, serviceProvider);
	}
	private static async Task UploadList(SocketModal component, IServiceProvider serviceProvider)
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

	private static string GetSelectedSession(SocketModal component)
	{
		var sessionSelectMenu = component.Data.Components.FirstOrDefault(o => 
			o.CustomId == "sessions");
		
		return sessionSelectMenu is null
			? throw new Exception("Cannot Find Session Select Menu")
			: sessionSelectMenu.Values.First();
	}
	/*private static DiscordDto.ModalComponent PrintLog(DiscordBotAdminSimpleAction simpleAction)
	{
		var modalComponent = new DiscordDto.ModalComponent(simpleAction.ModalTitle, simpleAction.ModalType.ToString())
		{
			components = [
				CreateSessionSelectMenuComponent(simpleAction)
			]
		};
		return modalComponent;
	}
	private static DiscordDto.ModalComponent ExportLog(DiscordBotAdminSimpleAction simpleAction)
	{
		var modalComponent = new DiscordDto.ModalComponent(simpleAction.ModalTitle, simpleAction.ModalType.ToString())
		{
			components = [
				CreateSessionSelectMenuComponent(simpleAction)
			]
		};
		return modalComponent;
	}*/
}
