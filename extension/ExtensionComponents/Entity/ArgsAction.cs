namespace ExtensionComponents.Entity;

public interface IArgsAction
{
	public IOutputBuilder Output { get; init; }
	public IArgsBuilder Args { get; init; }
	public nint FunctionPtr { get; init; }
	public (IOutputBuilder, string[], nint) GetParams();
}

public readonly record struct ArgsAction(IOutputBuilder Output, IArgsBuilder Args, nint FunctionPtr) : IArgsAction
{
	public (IOutputBuilder, string[], nint) GetParams() => (Output, Args.GetArgsStringArray(), FunctionPtr);
}
