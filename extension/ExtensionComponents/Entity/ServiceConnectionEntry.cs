namespace ExtensionComponents.Entity;

public interface ILocalServices
{
	void SetActionsMap(Type actionType);
	void Output(nint destination, int outputSize, string data);
	int ExecuteArgsAction(IArgsAction argsAction);
}
