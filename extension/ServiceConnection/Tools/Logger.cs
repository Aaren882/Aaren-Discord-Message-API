using ServiceConnection.Entity;

namespace ServiceConnection.Tools;

public class LoggerBase: ILogger
{
	private static readonly string ExtFilePath = Util.AssemblyPath!;
	private static readonly string LogFilePath = Path.Combine(ExtFilePath, "logs");
	private static readonly string LogFileName = Path.Combine(
		LogFilePath,
		$"{DateTime.Now:yyyy-MM-dd.HH-mm-ss}.log");

	public static void Trace(string Name, string content)
	{
#if DEBUG
		Log(null, $"TRACER - {Name} : {content}");
#endif
	}

	public static void Log(Exception? e, string s = "")
	{
		if (!Directory.Exists(ExtFilePath))
			Directory.CreateDirectory(ExtFilePath);
		if (!Directory.Exists(LogFilePath))
			Directory.CreateDirectory(LogFilePath);

		using var file = new StreamWriter(LogFileName, true);
		if (string.IsNullOrEmpty(s))
			s = e!.Message;
		
		if (s.Length > 0)
			file.WriteLine($"{DateTime.Now:T} - {s}");
	}

	public static void CleanLogs()
	{
		var limit = 10;
		var files = Directory.GetFiles(LogFilePath);

		//- Check how many logs
		Trace("CleanLogs", "Check how many logs...");
		if (files.Length < limit) return;

		Dictionary<string, DateTime> dict = new();
		foreach (var file in files)
		{
			var time = Directory.GetCreationTime(file);
			dict.Add(file, time);
		}

		var list = dict.OrderBy(x => x.Value).ToList();
		for (var i = 0; i < list.Count - limit; i++)
		{
			var logFile= list[i].Key;
			File.Delete(logFile);
		}
		
		Trace("CleanLogs", "Old logs is cleaned out.");
	}
}
