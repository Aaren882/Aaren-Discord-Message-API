using System.Reflection;
using System.Runtime.InteropServices;
using ExtensionComponents;
using ExtensionComponents.Entity;
using ExtensionComponents.Tools;
using Microsoft.Extensions.DependencyInjection;
using ServiceConnection;
using ServiceConnection.WebService;
using static ExtensionComponents.ExtensionStartup;

namespace DiscordMessageAPIService;

public sealed class DllEntry
{
	/* [UnmanagedCallersOnly(EntryPoint = "RVExtensionFeatureFlags")]
	public static ulong RVExtensionFeatureFlags()
	{
		return (ulong)(
			// RVFeatureFlags.ContextNoDefaultCall | //- Turn-off Call Context automatically
			RVFeatureFlags.ArgumentNoEscapeString
		);
	} */

	/// <summary>
	/// Receives context information .
	/// </summary>from Arma 3 about the execution environment
	/// <param name="argsPtr">Pointer to the array of strings containing context data.</param>
	/// <param name="argCount">The number of arguments passed in the context.</param>
	[UnmanagedCallersOnly(EntryPoint = "RVExtensionContext")]
	public static void RVExtensionContext(nint argsPtr, int argCount)
	{
		try
		{
			var context = ExtensionStartup.LocalServices?.GetCallContext(argsPtr, argCount);
			if (context is null)
				throw new NullReferenceException("CallContext parse failed.");

			ContextInfo = context;
			LoggerBase.Trace(nameof(ContextInfo), ContextInfo.ToString());
		}
		catch (Exception ex)
		{
			LoggerBase.Log(ex, nameof(RVExtensionContext));
		}
	}

	/// <summary>
	/// Register callback for Arma
	/// </summary>
	/// <param name="functionPtr"></param>
	[UnmanagedCallersOnly(EntryPoint = "RVExtensionRegisterCallback")]
	public static void RVExtensionRegisterCallback(nint functionPtr)
	{
		try
		{
			Callback = Marshal.GetDelegateForFunctionPointer<ExtensionCallback>(functionPtr);
			LoggerBase.Trace("RVExtensionRegisterCallback", "CallBack Initiated");
		}
		catch (Exception e)
		{
			LoggerBase.Log(e, "RVExtensionRegisterCallback");
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
		ServiceCollection services = new();
		services.AddSingleton<EntryDelegatesBase, EntryDelegates>();
		services.AddSingleton<ServiceInteractions>();
		services.AddSingleton<ILocalServices, LocalServices>();
		services.AddSingleton<ServiceRequestHandler>();
		services.AddSingleton<WebsocketClient>();
		services.SetupFileLogger();

		var serviceProvider = services.BuildServiceProvider();

		var version = typeof(DllEntry).GetTypeInfo().Assembly
				.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
				.InformationalVersion;

		version = version[..(version.LastIndexOf('+') + 9)];

		// Centralize configuration and initialization via the shared components startup class
		serviceProvider.InitConfiguration(LoggerBase.Trace, LoggerBase.Log);

		LoggerBase.Log(null, $"Extension Version : [{version}]");
		ExtensionStartup.LocalServices?.Output(outputPrt, outputSize, version);
	}

	/* /// <summary>
	/// The entry point for the default callExtension command.
	/// </summary>
	/// <param name="outputPrt">The string builder object that contains the result of the function</param>
	/// <param name="outputSize">The maximum size of bytes that can be returned</param>
	/// <param name="function">The string argument that is used along with callExtension</param>
	[UnmanagedCallersOnly(EntryPoint = "RVExtension")]
	public static void RVExtension(nint outputPrt, int outputSize, nint function)
	{
		// var inputKey = Marshal.PtrToStringUTF8(function)!;
		// ServiceStartup.localServices.Output(outputPrt, outputSize, inputKey);
	} */

	/// <summary>
	/// The entry point for the callExtensionArgs command.
	/// </summary>
	/// <param name="outputPrt"></param>
	/// <param name="outputSize"></param>
	/// <param name="functionPtr"></param>
	/// <param name="argsPrt"></param>
	/// <param name="argCount"></param>
	/// <returns>
	///     numbers
	/// </returns>
	[UnmanagedCallersOnly(EntryPoint = "RVExtensionArgs")]
	public static int RvExtensionArgs(nint outputPrt, int outputSize, nint functionPtr, nint argsPrt, int argCount)
	{
		return
			ExtensionStartup.LocalServices?.ExecuteArgsAction(outputPrt, outputSize, functionPtr, argsPrt, argCount)
			?? -1;
	}
}
