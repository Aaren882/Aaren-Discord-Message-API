using Components.Entity;
using DiscordMessageAPI.DiscordMessageAPI.WebService;
using ServiceConnection.Discord;

namespace ServiceConnection;

public class ServiceConnectionEntry
{
	public static string? InitTime { get; set; } = null;
	public static bool ExtensionInit { get; set; } = false;
	public static WebhooksStorage? ALLWebhooks = null;
	public static readonly ServiceInteractions ServiceInteractions = new();
	
	public static CallContext contextInfo;
	public static ExtensionCallback Callback;
}
