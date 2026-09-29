using System.Reflection;
using System.Runtime.InteropServices;
using ExtensionComponents;
using ExtensionComponents.Entity;
using ExtensionComponents.Tools;
using Microsoft.Extensions.DependencyInjection;
using static ExtensionComponents.ExtensionStartup;

namespace DiscordMessageAPI;

public sealed class DllEntry
{
	[UnmanagedCallersOnly(EntryPoint = "RVExtensionFeatureFlags")]
	public static ulong RVExtensionFeatureFlags()
	{
		return (ulong)(RVFeatureFlags.ContextNoDefaultCall | RVFeatureFlags.ArgumentNoEscapeString);
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
		services.AddSingleton<ILocalServices, LocalServices>();
		services.AddSingleton<EntryDelegatesBase, EntryDelegates>();

		//- Assembly Info
		var assembly = typeof(DllEntry).GetTypeInfo().Assembly;
		var assemblyName = assembly.GetName().Name!;
		services.SetupFileLogger(assemblyName);

		var version = assembly
			.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
			.InformationalVersion;
		version = version[..(version.LastIndexOf('+') + 9)];

		//- Setup Service Configuration
		var serviceProvider = services.BuildServiceProvider();
		serviceProvider.InitConfiguration();

		LoggerBase.Log(null, $"\"{assemblyName}\" Extension Version : [{version}]");
		ExtensionStartup.LocalServices?.Output(outputPrt, outputSize, version);
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
		// var inputKey = Marshal.PtrToStringUTF8(function)!;
		// ServiceStartup.localServices.Output(outputPrt, outputSize, inputKey);
	}

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
