using ServiceConnection.Discord;
using ServiceConnection.Tools;
using ServiceConnection.WebService;

namespace ServiceConnection.Entity;

public interface ILocalServices
{
	void Output(nint destination, int outputSize, string data);
	int ExecuteArgsAction(IArgsAction argsAction);
}
