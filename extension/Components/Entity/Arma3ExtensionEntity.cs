using System.Runtime.InteropServices;
using System.Text;

namespace Components.Entity;

public record struct CallContext(
	UInt64 steamId,
	string fileSource,
	string missionName,
	string serverName,
	Int16 remoteExecutedOwner
);

public delegate int ExtensionCallback(
	string name, 
	string function,
	string data
);

public record OutputBuilder(nint destination, int outputSize)
{
	/// <summary>
	/// Construct output buffer for Arma
	/// </summary>
	/// <param name="data">String data that will be output</param>
	public void Append(string data)
	{
		Output(destination, outputSize, data);
	}
	
	public static void Output(IntPtr destination, int outputSize, string data)
	{
		var buffer = new byte[outputSize];
		//- Empty buffer (clean up previous output)
		Marshal.Copy(buffer, 0, destination, outputSize);
			
		//- Write data into buffer 
		var bytes = Encoding.UTF8.GetBytes(data, buffer);
		Marshal.Copy(buffer, 0, destination, bytes);
	}
}
