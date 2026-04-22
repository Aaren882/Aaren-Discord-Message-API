using System.Net.Sockets;
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
	public static ExtensionCallback? Callback = (name, function, data) => 0;

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
		serviceInteractions = serviceProvider.GetService<ServiceInteractions>();

		try
		{
			localServices = serviceProvider.GetRequiredService<ILocalServices>();
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
