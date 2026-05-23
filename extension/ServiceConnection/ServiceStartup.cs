using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using ServiceConnection.Entity;
using ServiceConnection.Tools;
using ServiceConnection.WebService;

namespace ServiceConnection;

public static class ServiceStartup
{
	internal static Action<string, string> Tracer { get; private set; }
	internal static Action<Exception?, string> Logger { get; private set; }
	public static string? InitTime { get; set; }
	
	public static bool ExtensionInit { get; private set; }
	internal static DateTime ExtensionInitTime = DateTime.Now; //- must be static
	public static bool ExtensionWebhookInit { get; set; }
	public static WebhooksStorage? ALLWebhooks { get; set; }
	public static string? RptFileDirectory { get; internal set; }
	
	public static CallContext ContextInfo { get; set; }
	public static ExtensionCallback Callback = (name, function, data) => 0;

	public static ServiceInteractions? serviceInteractions { get; private set; }
	
	public static IServiceProvider ServiceProvider { get; private set; }
	public static ILocalServices localServices { get; private set; }

	public static void InitConfiguration(
		Action<string, string> tracer, 
		Action<Exception?, string> logger,
		IServiceProvider serviceProvider
	)
	{
		Tracer = tracer;
		Logger = logger;
		ServiceProvider = serviceProvider;
		serviceInteractions = serviceProvider.GetService<ServiceInteractions>();

		try
		{
			localServices = serviceProvider.GetRequiredService<ILocalServices>();
			if (serviceInteractions != null)
			{
				RptFileDirectory = Util.GetCurrentRpt();
				Logger(null, "Registered RPT File : " + RptFileDirectory);
			}
			Tracer(nameof(localServices), "Local Services Initialized");
		}
		catch (Exception e) when (e is SocketException or HttpRequestException)
		{
			Logger(e, "No Backend Connection.");
		}
		catch (Exception e)
		{
			Logger(e, "Initialization Failed");
		}
	}
	
	public static async Task InitializeAsync(string accessName, string? profilePayload = null)
	{
		if (serviceInteractions == null)
		{
			throw new InvalidOperationException("ServiceInteractions not initialized. Call InitConfiguration first.");
		}

		ExtensionInit = true;
		
		//- Create
		try
		{
			Logger(null, "Initializing WebSocket Connection");
			await serviceInteractions.EstablishWebSocketConnection(accessName, profilePayload ?? string.Empty);
		}
		catch (Exception e) when (e is SocketException or HttpRequestException)
		{
			Logger(e, "No Backend Connection.");
		}
		catch (Exception e)
		{
			Logger(e, null);
		}
	}

	public static async Task ShutdownAsync()
	{
		if (serviceInteractions == null)
		{
			throw new InvalidOperationException("ServiceInteractions not initialized. Call InitConfiguration first.");
		}
		
		Logger(null, "Shutting down WebSocket Connection");
		await serviceInteractions.DisconnectWebSocket("Extension Shutting Down");
		ExtensionInit = false;
	}
}
