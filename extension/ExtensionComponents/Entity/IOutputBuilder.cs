namespace ExtensionComponents.Entity;

public interface IOutputBuilder
{
	public nint DestinationPtr { get; init; }
	public int OutputSize { get; init; }

	/// <summary>
	/// Construct output buffer for Arma
	/// </summary>
	/// <param name="data">String data that will be output</param>
	public void Append(string data);
}

public readonly record struct OutputBuilder(nint DestinationPtr, int OutputSize) : IOutputBuilder
{
	public void Append(string data)
	{
		ExtensionStartup.LocalServices?.Output(DestinationPtr, OutputSize, data);
	}
}
