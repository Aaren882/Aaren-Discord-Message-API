using System.Runtime.InteropServices;
using System.Text;
using ServiceConnection.Entity;
using ServiceConnection.WebService;

namespace ServiceConnection;

public class LocalServices: ILocalServices
{
	public void Output(nint destination, int outputSize, string data)
	{
		var buffer = new byte[outputSize];

		//- Empty buffer (clean up previous output)
		Marshal.Copy(buffer, 0, destination, outputSize);

		//- Write data into buffer
		var bytes = Encoding.UTF8.GetBytes(data, buffer);
		Marshal.Copy(buffer, 0, destination, bytes);
	}
	
	public int ExecuteArgsAction(IArgsAction argsAction)
	{
		try
		{
			return argsAction.ExecuteAction();
		}
		catch (Exception e)
		{
			argsAction.Output.Append($"Error!! \"{e.Message}\"");
			ServiceStartup.Logger(e, null);

			return -11;
		}
	}
}
