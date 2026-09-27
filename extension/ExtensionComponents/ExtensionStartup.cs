using ExtensionComponents.Entity;
using ExtensionComponents.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace ExtensionComponents;

public static class ExtensionStartup
{
	public static Action<string, string> Tracer { get; private set; } = LoggerBase.Trace;

	public static Action<Exception?, string> Logger { get; private set; } = LoggerBase.Log;

	public static string? InitTime { get; set; }

	public static bool ExtensionWebhookInit { get; set; }
	public static WebhooksStorage? ALLWebhooks { get; set; }

	public static CallContext ContextInfo { get; set; }
	public static ExtensionCallback Callback = (name, function, data) => 0;

	public static IServiceProvider? ServiceProvider { get; private set; }
	public static ILocalServices? LocalServices { get; private set; }

	public static void InitConfiguration(IServiceProvider serviceProvider)
	{
		ServiceProvider = serviceProvider;

		try
		{
			LocalServices = serviceProvider.GetRequiredService<ILocalServices>();
			Logger(null, $"({nameof(ExtensionStartup)}) Local Services Initialized");
		}
		catch (Exception e)
		{
			Logger(e, "Initialization Failed");
		}
	}
	public static void SetDefaultLoggers(
		Action<string, string> tracer,
		Action<Exception?, string> logger
	)
	{
		Tracer = tracer;
		Logger = logger;
	}
}
