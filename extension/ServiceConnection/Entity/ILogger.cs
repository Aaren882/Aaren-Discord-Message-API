namespace ServiceConnection.Entity;

public interface ILogger
{
	/// <summary>
	/// Writes a trace message to the logger if debugging is enabled.
	/// </summary>
	/// <remarks>No output is generated if debugging is not enabled. This method is intended for
	/// internal diagnostic purposes.</remarks>
	/// <param name="Name">The name or category associated with the trace message. Used to identify the source or context of the trace
	/// output.</param>
	/// <param name="content">The content of the trace message to be logged.</param>
	static abstract void Trace(string Name, string content);
	static abstract void Log(Exception? e, string s = "");
	static abstract void CleanLogs();
}
