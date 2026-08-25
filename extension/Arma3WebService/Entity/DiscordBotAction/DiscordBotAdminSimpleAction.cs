using System.Text.Json.Serialization;

namespace Arma3WebService.Entity.DiscordBotAction;

public enum DiscordBotAdminModalType
{
	upload_list,
	print_log,
	export_log,
	admin_mp_command,
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
	public string ModalTitle { get; set; } = string.Empty;
	public string? ComponentTitle { get; set; }
	public string? Description { get; set; }
	public SessionMenuOptions SessionMenu { get; set; } = new("Game Session");
	public IEnumerable<string>? ConnectionsNames { get; set; }
}
