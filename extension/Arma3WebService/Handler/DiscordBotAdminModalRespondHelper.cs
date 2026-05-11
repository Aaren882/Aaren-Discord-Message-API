using Arma3WebService.Entity;
using Arma3WebService.Entity.DiscordBotAction;
using Discord;
using Discord.WebSocket;

namespace Arma3WebService.Handler;

internal static class DiscordBotAdminModalRespondHelper
{
	internal const string SessionSelectMenuComponentCustomId = "sessions";
	private delegate DiscordDto.ModalComponent RespondAction(DiscordBotAdminSimpleAction simpleAction);
	private static readonly Dictionary<DiscordBotAdminModalType, IEnumerable<string>> ComponentCustomId = new Dictionary<DiscordBotAdminModalType, IEnumerable<string>> 
	{
		{ DiscordBotAdminModalType.upload_list, ["file_upload"] },
		{ DiscordBotAdminModalType.admin_broadcast, ["broadcast_inputText"] }
	};
	
	public static IEnumerable<string> GetComponentCustomId(this DiscordBotAdminModalType modalType)
		=> ComponentCustomId[modalType];
	
	public static async Task Extension(this DiscordBotAdminSimpleAction simpleAction, SocketMessageComponent component)
	{
		RespondAction content = (simpleAction.ModalType) switch
		{
			DiscordBotAdminModalType.upload_list => UploadList,
			DiscordBotAdminModalType.print_log => PrintLog,
			DiscordBotAdminModalType.export_log => ExportLog,
			DiscordBotAdminModalType.admin_restart_mission => AdminRestartMission,
			DiscordBotAdminModalType.admin_broadcast => AdminBroadcast,
			_ => throw new ArgumentOutOfRangeException(nameof(simpleAction), "\"ModalType\" for this ModalRespond does not exist.")
		};
		var modal = content(simpleAction).Build();
		await component.RespondWithModalAsync(modal);
	}
	
	private static DiscordDto.ModalComponent UploadList(DiscordBotAdminSimpleAction simpleAction)
	{
		var componentCustomId = simpleAction.ModalType.GetComponentCustomId().First();
		var label = new DiscordDto.LabelComponent
		{
			label = simpleAction.ComponentTitle ?? "File Upload",
			description = simpleAction.Description,
			component = new DiscordDto.FileUploadComponent(componentCustomId)
		};
		var modalComponent = new DiscordDto.ModalComponent(simpleAction.ModalTitle, simpleAction.ModalType.ToString())
		{
			components = [
				CreateSessionSelectMenuComponent(simpleAction), 
				label
			]
		};
		return modalComponent;
	}
	private static DiscordDto.ModalComponent PrintLog(DiscordBotAdminSimpleAction simpleAction)
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
	}
	private static DiscordDto.ModalComponent AdminRestartMission(DiscordBotAdminSimpleAction simpleAction)
	{
		var modalComponent = new DiscordDto.ModalComponent(simpleAction.ModalTitle, simpleAction.ModalType.ToString())
		{
			components = [
				CreateSessionSelectMenuComponent(simpleAction)
			]
		};
		return modalComponent;
	}
	private static DiscordDto.ModalComponent AdminBroadcast(DiscordBotAdminSimpleAction simpleAction)
	{
		var customId = simpleAction.ModalType.GetComponentCustomId().First();
		var label = new DiscordDto.LabelComponent
		{
			label = simpleAction.ComponentTitle ?? "Broadcast Message",
			description = simpleAction.Description,
			component = new DiscordDto.TextInputComponent(customId, null, 1, 2000, true, null, TextInputStyle.Paragraph)
		};
		var modalComponent = new DiscordDto.ModalComponent(simpleAction.ModalTitle, simpleAction.ModalType.ToString())
		{
			components = [
				CreateSessionSelectMenuComponent(simpleAction),
				label
			]
		};
		return modalComponent;
	}
	
	private static DiscordDto.LabelComponent CreateSessionSelectMenuComponent(DiscordBotAdminSimpleAction simpleAction)
	{
		var options = simpleAction.ConnectionsNames?.Select(
			name => new DiscordDto.SelectMenuOption(
				name, name, null, null
			)
		);
		
		return new DiscordDto.LabelComponent
		{
			label = simpleAction.SessionMenu.Label,
			description = simpleAction.SessionMenu.Description,
			component = new DiscordDto.SelectMenuComponent
			{
				custom_Id = SessionSelectMenuComponentCustomId,
				options = options
			}
		};
	}
}
