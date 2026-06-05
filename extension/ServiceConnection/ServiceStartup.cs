using System.Net.Sockets;
using ExtensionComponents;
using Microsoft.Extensions.DependencyInjection;
using ServiceConnection.WebService;
using ServiceConnection.Tools;

namespace ServiceConnection;

public static class ServiceStartup
{
    public static bool ExtensionInit { get; private set; }
    internal static DateTime ExtensionInitTime = DateTime.Now; //- must be static
	
	public static string? RptFileDirectory { get; set; }

	public static ServiceInteractions? serviceInteractions { get; private set; }

	public static void InitConfiguration(
		Action<string, string> tracer, 
		Action<Exception?, string> logger,
		IServiceProvider serviceProvider
	)
	{
		ExtensionStartup.InitConfiguration(tracer, logger, serviceProvider); //- Init Extension Configuration
		serviceInteractions = serviceProvider.GetService<ServiceInteractions>();

		try
		{
			if (serviceInteractions != null)
			{
				RptFileDirectory = Util.GetCurrentRpt();
				ExtensionStartup.Logger(null, "Registered RPT File : " + RptFileDirectory);
			}

			ExtensionStartup.Tracer(nameof(ExtensionStartup.localServices), "Local Services Initialized");
		}
		catch (Exception e) when (e is SocketException or HttpRequestException)
		{
			ExtensionStartup.Logger(e, "No Backend Connection.");
		}
		catch (Exception e)
		{
			ExtensionStartup.Logger(e, "Initialization Failed");
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
			ExtensionStartup.Logger(null, "Initializing WebSocket Connection");
			await serviceInteractions.EstablishWebSocketConnection(accessName, profilePayload ?? string.Empty);
		}
		catch (Exception e) when (e is SocketException or HttpRequestException)
		{
			ExtensionStartup.Logger(e, "No Backend Connection.");
		}
		catch (Exception e)
		{
			ExtensionStartup.Logger(e, null);
		}
	}

	public static async Task ShutdownAsync()
	{
		if (serviceInteractions == null)
		{
			throw new InvalidOperationException("ServiceInteractions not initialized. Call InitConfiguration first.");
		}

		ExtensionStartup.Logger(null, "Shutting down WebSocket Connection");
		await serviceInteractions.DisconnectWebSocket("Extension Shutting Down");
		ExtensionInit = false;
	}
}
