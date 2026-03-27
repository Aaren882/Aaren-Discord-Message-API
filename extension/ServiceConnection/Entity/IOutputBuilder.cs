using static ServiceConnection.ServiceConnectionEntry;

namespace ServiceConnection.Entity;

public interface IOutputBuilder
{
	public nint Destination { get; set; }
	public int OutputSize { get; set; }
	
	public void Append(string data);
}

public record struct OutputBuilder(nint Destination, int OutputSize): IOutputBuilder
{
	/// <summary>
	/// Construct output buffer for Arma
	/// </summary>
	/// <param name="data">String data that will be output</param>
	public void Append(string data)
	{
		Output(Destination, OutputSize, data);
	}
}
