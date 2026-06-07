using static ExtensionComponents.LocalServices;
namespace ExtensionComponents.Entity;

public interface IArgsAction
{
	public IOutputBuilder Output { get; init; }
	public string[] Args { get; init; }
	public string FunctionName { get; init; }
	
	public (IOutputBuilder, string[], string) GetParams();
}

public readonly record struct ArgsAction(IOutputBuilder Output, string[] Args, string FunctionName) : IArgsAction
{
	public (IOutputBuilder, string[], string) GetParams() => (Output, Args, FunctionName);
}
