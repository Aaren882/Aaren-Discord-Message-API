using Microsoft.Extensions.DependencyInjection;
using ServiceConnection.Entity;
using ServiceConnection.WebService;

namespace ServiceConnection;

public class ServiceStartup
{
	internal static Action<string, string> Tracer;
	internal static Action<Exception?, string> Logger;
	public static string? InitTime;
	
	public static bool ExtensionInit { get; set; }
	public static WebhooksStorage? ALLWebhooks;
	
	public static CallContext ContextInfo;
	public static ExtensionCallback? Callback;

	public static ServiceInteractions? serviceInteractions;
	
	public static IServiceProvider ServiceProvider;
	public static ILocalServices localServices;

	public static void InitConfiguration(
		Action<string, string> tracer, 
		Action<Exception?, string> logger,
		IServiceProvider serviceProvider
	)
	{
		Tracer = tracer;
		Logger = logger;
		ServiceProvider = serviceProvider;

		try
		{
			serviceInteractions = serviceProvider.GetService<ServiceInteractions>();
			localServices = serviceProvider.GetRequiredService<ILocalServices>();
		}
		catch (Exception e)
		{
			Logger(e, "Initialization Failed");
		}
	}
	
	public static async Task InitializeAsync(string accessName)
	{
		if (serviceInteractions == null)
		{
			throw new InvalidOperationException("ServiceInteractions not initialized. Call InitConfiguration first.");
		}

		await serviceInteractions.EstablishWebSocketConnection(accessName);
		ExtensionInit = true;
		InitTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
	}

	public static async Task ShutdownAsync()
	{
		if (serviceInteractions == null)
		{
			throw new InvalidOperationException("ServiceInteractions not initialized. Call InitConfiguration first.");
		}
		
		await serviceInteractions.DisconnectWebSocket("Extension Shutting Down");
		ExtensionInit = false;
	}
}
