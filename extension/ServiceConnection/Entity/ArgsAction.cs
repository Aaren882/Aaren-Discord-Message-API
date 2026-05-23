using static ServiceConnection.Delegates.EntryDelegates;
using static ServiceConnection.LocalServices;

namespace ServiceConnection.Entity;

public interface IArgsAction
{
	public IOutputBuilder Output { get; init; }
	public string[] Args { get; init; }
	public string FunctionName { get; init; }
	
	public InitActions GetAction();
	public int ExecuteAction();
}

public readonly record struct ArgsAction(IOutputBuilder Output, string[] Args, string FunctionName) : IArgsAction
{
	public InitActions GetAction()
	{
		ServiceStartup.Tracer("DLL Entry", FunctionName);

		if (!ActionsDict.TryGetValue(FunctionName, out var action))
			throw new NullReferenceException($"Function \"{FunctionName}\" is not exist.");
		
		return action;
	}

	public int ExecuteAction()
	{
		var action = GetAction();
		
		var result= action(Output, Args, Args.Length);
		return result;
	}
};
