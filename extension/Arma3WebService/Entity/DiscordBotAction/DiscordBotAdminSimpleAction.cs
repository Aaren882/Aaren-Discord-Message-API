using System.Net.Mime;
using System.Text.Json.Serialization;
using Arma3WebService.DBContext;
using Arma3WebService.Handler;
using Arma3WebService.Managers;
using Discord.WebSocket;

namespace Arma3WebService.Entity.DiscordBotAction;

public enum DiscordBotAdminModalType
{
	upload_list,
	print_log,
	export_log,
	admin_restart_mission,
	admin_broadcast,
}

public record SessionMenuOptions(
	[property: JsonPropertyName("Title")] string Label,
	string? Description = null
);

public record DiscordBotAdminSimpleAction : DiscordBotActionBase
{
	[JsonIgnore]
	public DiscordBotAdminModalType ModalType { get; set; }
	public string ModalTitle { get; set; }
	public string ComponentTitle { get; set; }
	public string? Description { get; set; }
	public SessionMenuOptions SessionMenu { get; set; } = new("Game Session");
	public IEnumerable<string> ConnectionsNames { get; set; }
	
	public override async Task Run(SocketMessageComponent component)
	{
		DiscordBotAdminModalRespondHelper.RespondAction content = (ModalType) switch
		{
			DiscordBotAdminModalType.upload_list => DiscordBotAdminModalRespondHelper.UploadList,
			DiscordBotAdminModalType.print_log => DiscordBotAdminModalRespondHelper.PrintLog,
			DiscordBotAdminModalType.export_log => DiscordBotAdminModalRespondHelper.ExportLog,
			_ => throw new ArgumentOutOfRangeException()
		};
		var modal = content(this).Build();
		await component.RespondWithModalAsync(modal);
	}
	public async Task Run(SocketModal component, IServiceProvider serviceProvider)
	{
		DiscordBotAdminSubmitHelper.SubmitAction content = (ModalType) switch
		{
			DiscordBotAdminModalType.upload_list => DiscordBotAdminSubmitHelper.UploadList,
			// DiscordBotAdminModalType.print_log => DiscordBotAdminSubmitHelper.PrintLog,
			// DiscordBotAdminModalType.export_log => DiscordBotAdminSubmitHelper.ExportLog,
			_ => throw new ArgumentOutOfRangeException()
		};
		await content(component, serviceProvider);
	}
}
