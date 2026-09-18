using System.Net.Sockets;
using ExtensionComponents;
using Microsoft.Extensions.DependencyInjection;
using ServiceConnection.Tools;
using ServiceConnection.WebService;

namespace ServiceConnection;

public static class ServiceStartup
{
	public static bool ExtensionInit { get; private set; }
	internal static DateTime ExtensionInitTime = DateTime.Now; //- must be static
	private static string? _RptFileDirectory { get; set; }
	public static string RptFileDirectory
	{
		get => _RptFileDirectory ?? throw new NullReferenceException($"{nameof(RptFileDirectory)} has not been set.");
		set => _RptFileDirectory = value;
	}

	private static ServiceInteractions? _ServiceInteractions { get; set; }
	public static ServiceInteractions ServiceInteractions
	{
		get => _ServiceInteractions ?? throw new NullReferenceException("ServiceInteractions has not been set.");
		private set => _ServiceInteractions = value;
	}

	public static void InitConfiguration(
		this IServiceProvider serviceProvider,
		Action<string, string> tracer,
		Action<Exception?, string> logger
	)
	{
		ExtensionStartup.SetDefaultLoggers(tracer, logger); //- Init Default Logger
		ExtensionStartup.InitConfiguration(serviceProvider); //- Init Extension Configuration
		_ServiceInteractions = serviceProvider.GetService<ServiceInteractions>();

		try
		{
			if (ServiceInteractions != null)
			{
				RptFileDirectory = Util.GetCurrentRpt();
				ExtensionStartup.Logger(null, "Registered RPT File : " + RptFileDirectory);
			}

			ExtensionStartup.Tracer(nameof(ExtensionStartup.LocalServices), "Local Services Initialized");
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
		if (ServiceInteractions == null)
		{
			throw new InvalidOperationException("ServiceInteractions not initialized. Call InitConfiguration first.");
		}

		ExtensionInit = true;

		//- Create
		try
		{
			ExtensionStartup.Logger(null, "Initializing WebSocket Connection");
			await ServiceInteractions.EstablishWebSocketConnection(accessName, profilePayload ?? string.Empty);
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
		if (ServiceInteractions == null)
		{
			throw new InvalidOperationException("ServiceInteractions not initialized. Call InitConfiguration first.");
		}

		ExtensionStartup.Logger(null, "Shutting down WebSocket Connection");
		await ServiceInteractions.DisconnectWebSocket("Extension Shutting Down");
		ExtensionInit = false;
	}
}
