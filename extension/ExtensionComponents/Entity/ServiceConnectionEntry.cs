namespace ExtensionComponents.Entity;

[Flags]
public enum RVFeatureFlags : ulong
{
	None = 0,
	ContextArgumentsVoidPtr = 1 << 0, // 1
	ContextStackTrace = 1 << 1, // 2
	ContextNoDefaultCall = 1 << 2, // 4
	ArgumentNoEscapeString = 1 << 3, //- 8
}

public interface ILocalServices
{
	void Output(nint destination, int outputSize, string data);
	CallContext? GetCallContext(nint argsPtr, int argCount);
	int ExecuteArgsAction(nint outputPrt, int outputSize, nint functionPtr, nint argsPrt, int argCount);
	int ExecuteArgsAction(IArgsAction argsAction);
	ReadOnlySpan<byte> GetUtf8Span(nint pointer);
}
