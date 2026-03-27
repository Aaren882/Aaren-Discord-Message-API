using System.Reflection;
using System.Runtime.InteropServices;
using ServiceConnection;
using ServiceConnection.Entity;
using ServiceConnection.Tools;
using ServiceConnection.WebService;

namespace DiscordMessageAPI;

public class DllEntry
{
	/// <summary>
	/// Register callback for Arma
	/// </summary>
	/// <param name="functionPtr"></param>
	[UnmanagedCallersOnly(EntryPoint = "RVExtensionRegisterCallback")]
	public static void RVExtensionRegisterCallback(nint functionPtr)
	{
		try
		{
			ServiceConnectionEntry.Callback = Marshal.GetDelegateForFunctionPointer<ExtensionCallback>(functionPtr);
			LoggerBase.Trace("RVExtensionRegisterCallback", "CallBack Initiated");
		}
		catch (Exception e)
		{
			LoggerBase.Trace("RVExtensionRegisterCallback", "ERROR...");
			LoggerBase.Log(e);
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
		LoggerBase.CleanLogs();
		
		//- Setup Service Configuration
		ServiceConnectionEntry.InitConfiguration(
			LoggerBase.Trace,
			LoggerBase.Log
		);
		
		var version = typeof(DllEntry).GetTypeInfo().Assembly 
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
			.InformationalVersion;
		
		version = version
			.Substring(0, version.LastIndexOf('+') + 9);
		
		LoggerBase.Log(null, $"Extension Version : [{version}]");
		ServiceConnectionEntry.Output(outputPrt, outputSize, version);
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

		ServiceConnectionEntry.ContextInfo = new CallContext(
			Convert.ToUInt64(args[0]),
			args[1]!,
			args[2]!,
			args[3]!,
			Convert.ToInt16(args[4])
		);
		LoggerBase.Trace(nameof(ServiceConnectionEntry.ContextInfo),ServiceConnectionEntry.ContextInfo.ToString());
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

		try
		{
			ServiceConnectionEntry.Callback!("CallBack Name", inputKey, "data");
		}
		catch (Exception e)
		{
			LoggerBase.Log(e);
		}

		ServiceConnectionEntry.Output(outputPrt, outputSize, inputKey);
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
		var args = new string[argCount];
		for (var i = 0; i < argCount; i++)
		{
			var str = Marshal.PtrToStringUTF8(
					Marshal.ReadIntPtr(argsPrt + (i * Marshal.SizeOf<nint>()))
				)!
				.Trim('"', ' ') //- Remove Arma quotations
				.Replace("\"\"", "\"");

			args[i] = str;
			LoggerBase.Trace($"DLL Entry => \"{i}\"", $"\"str = {str}\"");
			//args = args.Select(arg => arg.Trim('"', ' ').Replace("\"\"", "\"")).ToArray();
		}
		
		var functionName = Marshal.PtrToStringUTF8(function)!;
		var output = new OutputBuilder(outputPrt, outputSize);
		var argsAction = new ArgsAction(output, args, functionName);

		return ServiceConnectionEntry.ExecuteArgsAction(argsAction);
	}
}
