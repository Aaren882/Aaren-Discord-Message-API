using System.Runtime.InteropServices;
using System.Text;
using ServiceConnection.Discord;
using ServiceConnection.Entity;
using ServiceConnection.Tools;
using ServiceConnection.WebService;

namespace ServiceConnection;

public class ServiceConnectionEntry
{
	internal static Action<string, string> Tracer;
	internal static Action<Exception?, string> Logger;
	
	public static string? InitTime;
	public static bool ExtensionInit { get; set; }
	public static WebhooksStorage? ALLWebhooks;
	public static ServiceInteractions? serviceInteractions;
	
	public static CallContext ContextInfo;
	public static ExtensionCallback? Callback;

	public static void Output(nint destination, int outputSize, string data)
	{
		var buffer = new byte[outputSize];

		//- Empty buffer (clean up previous output)
		Marshal.Copy(buffer, 0, destination, outputSize);

		//- Write data into buffer
		var bytes = Encoding.UTF8.GetBytes(data, buffer);
		Marshal.Copy(buffer, 0, destination, bytes);
	}

	public static void InitConfiguration(
		Action<string, string> tracer, 
		Action<Exception?, string> logger
	)
	{
		Tracer = tracer;
		Logger = logger;

		try
		{
			serviceInteractions = new ServiceInteractions();
		}
		catch (Exception e)
		{
			logger(e, null);
		}
	}
	
	public static int ExecuteArgsAction(IArgsAction argsAction)
	{
		try
		{
			return argsAction.ExecuteAction();
		}
		catch (Exception e)
		{
			argsAction.Output.Append($"Error!! \"{e.Message}\"");
			Logger(e, null);

			return -11;
		}
	}
}
