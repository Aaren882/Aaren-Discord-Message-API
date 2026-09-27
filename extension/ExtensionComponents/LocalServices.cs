using System.Runtime.InteropServices;
using System.Text;
using ExtensionComponents.Entity;
using Microsoft.Extensions.Logging;

namespace ExtensionComponents;

public class LocalServices(ILogger<LocalServices> Logger, EntryDelegatesBase entryDelegates) : ILocalServices
{
	public unsafe void Output(nint destination, int outputSize, string data)
	{
		//- Execution Time: 0.1319 ms  |  Cycles: 7583/10000  (ORIGIN)
		//- Execution Time: 0.1290 ms  |  Cycles: 7751/10000  (Improved "output()")
		//- Execution Time: 0.1164 ms  |  Cycles: 8593/10000  (Improved "output()" + Improved Args parsing)
		//- Execution Time: 0.0713 ms  |  Cycles: 10000/10000 (Improved "output()" + Improved Args parsing + Improved RVContext parsing)
		//- Execution Time: 0.0293 ms  |  Cycles: 10000/10000 (Improved "output()" + Improved Args parsing + "RVFeature_ArgumentNoEscapeString" + "RVFeature_RvContextNoDefaultCall")
		//- Execution Time: 0.0084 ms  |  Cycles: 10000/10000 (Improved "output()" + Improved Args parsing + "RVFeature_ArgumentNoEscapeString" + NO RVExtensionContext)
		try
		{
			//- less overhead
			Span<byte> output = new((byte*)destination, outputSize);

			//- Empty buffer (clean up previous output)
			output.Clear();

			//- Write data into buffer
			int bytesWritten = Encoding.UTF8.GetBytes(data, output);
			output[bytesWritten] = 0; //- add "/0" C-String
		}
		catch (Exception ex)
		{
			Logger.LogError(ex, "Error occurred while writing data to output buffer.");
		}
	}

	public unsafe CallContext? GetCallContext(nint argsPtr, int argCount)
	{
		if (argsPtr == nint.Zero) return null;

		var sourcePtr = (byte**)argsPtr;
		ReadOnlySpan<nint> sourceSpan = new(sourcePtr, argCount);

		var result = new string[argCount];
		for (var i = 0; i < argCount; i++)
		{
			var rawString = Marshal.PtrToStringUTF8(sourceSpan[i]) ?? string.Empty;
			Logger.LogTrace("{GetCallContext} {VarName}: {rawString}", nameof(GetCallContext), nameof(rawString), rawString);
			result[i] = rawString;
		}

		return new CallContext(
			UInt64.Parse(result[0]),
			result[1],
			result[2],
			result[3],
			short.Parse(result[4])
		);
	}

	public int ExecuteArgsAction(nint outputPrt, int outputSize, nint functionPtr, nint argsPrt, int argCount)
	{
		OutputBuilder output = new(outputPrt, outputSize);
		ArgsBuilder args = new(argsPrt, argCount);
		ArgsAction argsAction = new(output, args, functionPtr);

		return ExecuteArgsAction(argsAction);
	}
	public unsafe int ExecuteArgsAction(IArgsAction argsAction)
	{
		Logger.LogDebug("ExecuteArgsAction(IArgsAction argsAction)");
		var (output, args, functionPtr) = argsAction.GetParams();
		Logger.LogDebug("{argsAction}", argsAction);

		try
		{
			var functionSpan = GetUtf8Span(functionPtr);
			var functionString = Encoding.UTF8.GetString(functionSpan);

			Logger.LogDebug("Calling Function : {FunctionName}", functionString);

			if (!entryDelegates.ActionsDict.TryGetValue(functionSpan, out var actionPtr))
				throw new NullReferenceException($"Function \"{functionString}\" is not exist.");

			Logger.LogDebug("Function Found! Passing arguments ({output}, {args}, {ArgsCount})", output, args, args.Length);

			var action = (delegate* managed<IOutputBuilder, string[], int, int>)actionPtr;
			return action(output, args, args.Length);
		}
		catch (Exception e)
		{
			output.Append($"Error!! \"{e.Message}\"");
			Logger.LogError(e, "Error during {MethodName} execution.", nameof(ExecuteArgsAction));

			return -11;
		}
	}
	public unsafe ReadOnlySpan<byte> GetUtf8Span(nint pointer)
	{
		if (pointer == nint.Zero) return [];

		var bytePtr = (byte*)pointer;
		var current = bytePtr;

		// 1. Find the length by scanning for the null terminator (0)
		while (*current != 0)
		{
			current++;
		}
		int length = (int)(current - bytePtr);

		// 2. Create the byte span directly from the memory address
		return new(bytePtr, length);
	}
}
