using System.Runtime.InteropServices;
using System.Text;
using ExtensionComponents.Entity;
using static ExtensionComponents.ExtensionStartup;

namespace ExtensionComponents;

public class LocalServices: ILocalServices
{
	public delegate int InitAction(IOutputBuilder output, string[] args, int argCount);
	public Dictionary<string, InitAction> ActionsDict { get; private set; }
	
	public void SetActionsMap(EntryDelegatesBase entryDelegates)
	{
		var actionType = entryDelegates.GetType();
		
		ActionsDict = entryDelegates.ActionsDict;
        Logger(null, $"({nameof(SetActionsMap)}) => {actionType.FullName} registered {ActionsDict.Count} key(s).");
	}
	
	public void Output(nint destination, int outputSize, string data)
	{
		var buffer = new byte[outputSize];

		//- Empty buffer (clean up previous output)
		// Marshal.Copy(buffer, 0, destination, outputSize); //- OLD
		unsafe //- less overhead
		{
			NativeMemory.Clear((void*)destination, (nuint)outputSize);
		}

		//- Write data into buffer
		var bytes = Encoding.UTF8.GetBytes(data, buffer);
		Marshal.Copy(buffer, 0, destination, bytes);
	}
	
	public int ExecuteArgsAction(IArgsAction argsAction)
	{
		var (output, args, functionName) = argsAction.GetParams();
		return ExecuteArgsAction(output, args, functionName);
	}
	public int ExecuteArgsAction(IOutputBuilder Output, string[] Args, string FunctionName)
	{
		try
		{
			ExtensionStartup.Tracer("DLL Entry", FunctionName);

			if (!ActionsDict.TryGetValue(FunctionName, out var action))
				throw new NullReferenceException($"Function \"{FunctionName}\" is not exist.");
		
			return action(Output, Args, Args.Length);
		}
		catch (Exception e)
		{
			Output.Append($"Error!! \"{e.Message}\"");
			ExtensionStartup.Logger(e, null);

			return -11;
		}
	}
}
