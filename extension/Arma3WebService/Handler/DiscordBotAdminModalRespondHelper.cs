using Arma3WebService.Entity;
using Arma3WebService.Entity.DiscordBotAction;

namespace Arma3WebService.Handler;

internal static class DiscordBotAdminModalRespondHelper
{
	public delegate DiscordDto.ModalComponent RespondAction(DiscordBotAdminSimpleAction simpleAction);
	
	public static DiscordDto.ModalComponent UploadList(DiscordBotAdminSimpleAction simpleAction)
	{
		var label = new DiscordDto.LabelComponent
		{
			label = simpleAction.ComponentTitle,
			description = simpleAction.Description,
			component = new DiscordDto.FileUploadComponent("file_upload")
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
	public static DiscordDto.ModalComponent PrintLog(DiscordBotAdminSimpleAction simpleAction)
	{
		var modalComponent = new DiscordDto.ModalComponent(simpleAction.ModalTitle, simpleAction.ModalType.ToString())
		{
			components = [
				CreateSessionSelectMenuComponent(simpleAction)
			]
		};
		return modalComponent;
	}
	public static DiscordDto.ModalComponent ExportLog(DiscordBotAdminSimpleAction simpleAction)
	{
		var modalComponent = new DiscordDto.ModalComponent(simpleAction.ModalTitle, simpleAction.ModalType.ToString())
		{
			components = [
				CreateSessionSelectMenuComponent(simpleAction)
			]
		};
		return modalComponent;
	}
	
	private static DiscordDto.LabelComponent CreateSessionSelectMenuComponent(DiscordBotAdminSimpleAction simpleAction)
	{
		var options = simpleAction.ConnectionsNames.Select(
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
				custom_Id = "sessions",
				options = options
			}
		};
	}
}
