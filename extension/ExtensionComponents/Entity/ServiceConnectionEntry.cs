namespace ExtensionComponents.Entity;

public interface ILocalServices
{
	void SetActionsMap(EntryDelegatesBase actionType);
	void Output(nint destination, int outputSize, string data);
	int ExecuteArgsAction(IArgsAction argsAction);
}
