using ServiceConnection.Discord;
using ServiceConnection.Tools;
using ServiceConnection.WebService;

namespace ServiceConnection.Entity;

public interface IServiceConnectionEntry
{
	public static string? InitTime { get; set; } = null;
	public static bool ExtensionInit { get; set; } = false;
	public static WebhooksStorage? ALLWebhooks = null;
	public static readonly ServiceInteractions serviceInteractions = new();
	
	public static CallContext ContextInfo;
	public static ExtensionCallback? Callback;
	public static readonly ILogger logger;
}
