using ServiceConnection.Entity;
using static ServiceConnection.Delegates.EntryDelegates;

namespace featureTest;

public record struct TestOutputBuilder(nint Destination, int OutputSize): IOutputBuilder
{
	/// <summary>
	/// Construct output buffer for Arma
	/// </summary>
	/// <param name="data">String data that will be output</param>
	public void Append(string data)
	{
	}
}

public readonly record struct TestArgsAction(IOutputBuilder Output, string[] Args, string FunctionName) : IArgsAction
{
	public InitActions GetAction()
	{
		return ActionsDict.TryGetValue(FunctionName, out var action)
			? action
			: throw new NullReferenceException($"Function \"{FunctionName}\" is not exist.");
	}

	public int ExecuteAction()
	{
		var action = GetAction();
		return action(Output, Args, Args.Length);
	}
};
