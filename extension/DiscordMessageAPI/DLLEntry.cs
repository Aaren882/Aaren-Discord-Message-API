using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using DiscordMessageAPI.Discord;
using DiscordMessageAPI.Entity;
using DiscordMessageAPI.Tools;
using DiscordMessageAPI.WebService;
using static DiscordMessageAPI.Delegates.EntryDelegates;

namespace DiscordMessageAPI;

internal record struct CallContext(
	UInt64 steamId,
	string fileSource,
	string missionName,
	string serverName,
	Int16 remoteExecutedOwner
);

public class DllEntry
{
	//private static readonly string SessionKey = Tools.GenTimeEncode();
	public static string InitTime = null;
	public static bool ExtensionInit = false;
	public static Webhooks_Storage? ALLWebhooks = null;
	private static CallContext contextInfo;
	internal static readonly ServiceInteractions ServiceInteractions = new();

	private static void Output(IntPtr destination, int outputSize, string data)
	{
		var buffer = new byte[outputSize];
		//- Empty buffer (clean up previous output)
		Marshal.Copy(buffer, 0, destination, outputSize);
		
		//- Write data into buffer 
		var bytes = Encoding.UTF8.GetBytes(data, buffer);
		Marshal.Copy(buffer, 0, destination, bytes);
	}

	public readonly record struct OutputBuilder(IntPtr destination, int outputSize)
	{
		/// <summary>
		/// Construct output buffer for Arma
		/// </summary>
		/// <param name="data">String data that will be output</param>
		public void Append(string data)
		{
			Output(destination, outputSize, data);
		}
	}

	/// <summary>
	/// Gets called when Arma starts up and loads all extension.
	/// It's perfect to load in static objects in a separate thread so that the extension doesn't need any separate initialization
	/// </summary>
	/// <param name="outputPrt"></param>
	/// <param name="outputSize"></param>
	[UnmanagedCallersOnly(EntryPoint = "RVExtensionVersion")]
	public static void RVExtensionVersion(nint outputPrt, int outputSize)
	{
		//- Clean up logs
		Logger.CleanLogs();
		
		
		
		Output(outputPrt, outputSize, "26.2.0");
	}
	
	/// <summary>
	/// Receives context information .
	/// </summary>from Arma 3 about the execution environment
	/// <param name="argsPtr">Pointer to the array of strings containing context data.</param>
	/// <param name="argCount">The number of arguments passed in the context.</param>
	[UnmanagedCallersOnly(EntryPoint = "RVExtensionContext")]
	public static void RVExtensionContext(nint argsPtr, int argCount)
	{
		var args = new string?[argCount];

		for (var i = 0; i < argCount; i++)
		{
			var str = Marshal.PtrToStringUTF8(Marshal.ReadIntPtr(argsPtr + (i * Marshal.SizeOf<nint>())));
			args[i] = str;
		}

		contextInfo = new CallContext(
			Convert.ToUInt64(args[0]),
			args[1],
			args[2],
			args[3],
			Convert.ToInt16(args[4])
		);
		Logger.Trace(nameof(contextInfo),contextInfo.ToString());
	}


	/// <summary>
	/// The entry point for the default callExtension command.
	/// </summary>
	/// <param name="outputPrt">The string builder object that contains the result of the function</param>
	/// <param name="outputSize">The maximum size of bytes that can be returned</param>
	/// <param name="function">The string argument that is used along with callExtension</param>
	[UnmanagedCallersOnly(EntryPoint = "RVExtension")]
	public static void RVExtension(nint outputPrt, int outputSize, nint function)
	{
		var inputKey = Marshal.PtrToStringUTF8(function)!;

		Output(outputPrt, outputSize, inputKey);
	}

	/// <summary>
	/// The entry point for the callExtensionArgs command.
	/// </summary>
	/// <param name="outputPrt"></param>
	/// <param name="outputSize"></param>
	/// <param name="function"></param>
	/// <param name="argsPrt"></param>
	/// <param name="argCount"></param>
	/// <returns>
	///     numbers
	/// </returns>
	[UnmanagedCallersOnly(EntryPoint = "RVExtensionArgs")]
	public static int RvExtensionArgs(nint outputPrt, int outputSize, nint function, nint argsPrt, int argCount)
	{
		OutputBuilder output = new(outputPrt, outputSize);

		var inputKey = Marshal.PtrToStringUTF8(function)!;
		var args = new string[argCount];

		for (var i = 0; i < argCount; i++)
		{
			var str = Marshal.PtrToStringUTF8(
					Marshal.ReadIntPtr(argsPrt + (i * Marshal.SizeOf<nint>()))
				)!
				.Trim('"', ' ') //- Remove Arma quotations
				.Replace("\"\"", "\"");

			args[i] = str;
			Logger.Trace($"DLL Entry => \"{i}\"", $"\"str = {str}\"");
			//args = args.Select(arg => arg.Trim('"', ' ').Replace("\"\"", "\"")).ToArray();
		}

		try
		{
			Logger.Trace("DLL Entry", inputKey);
			
			if (!ActionsDict.TryGetValue(inputKey, out var action))
				throw new NullReferenceException($"Function \"{inputKey}\" is not exist.");

			var actionReturn = action(output, args, argCount);
			
			return actionReturn;
		}
		catch (Exception e)
		{
			output.Append($"Error!! \"{e.Message}\"");
			Logger.Log(e);

			return -11;
		}
	}
}
