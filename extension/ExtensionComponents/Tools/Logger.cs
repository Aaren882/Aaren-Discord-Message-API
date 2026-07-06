using System.Threading.Tasks.Dataflow;
using ExtensionComponents.Entity;

namespace ExtensionComponents.Tools;

public sealed class LoggerBase: ILogger
{
	private static readonly string ExtFilePath = Util.AssemblyPath!;
	private static readonly string LogFilePath = Path.Combine(ExtFilePath, "logs");
	private static readonly string LogFileName = Path.Combine(
		LogFilePath,
		$"{DateTime.Now:yyyy-MM-dd.HH-mm-ss}.log");

	private record LoggerObject(Exception? e, string s)
	{
		public void WriteLog()
		{
			var s = this.s;
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
	};
	private static readonly ActionBlock<LoggerObject> _loggerProcess = new (o => o.WriteLog(),
		new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

	public static void Trace(string Name, string content)
	{
#if DEBUG
		Log(null, $"TRACER - {Name} : {content}");
#endif
	}

	public static void Log(Exception? e, string s = "")
	{
		_loggerProcess.Post(new LoggerObject(e, s));
	}

	public static void CleanLogs()
	{
		const int limit = 10;
		var files = Directory.GetFiles(LogFilePath);

		//- Check how many logs
		Trace(nameof(CleanLogs), "Checking how many logs...");
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
		
		Trace(nameof(CleanLogs), "Old logs are cleaned out.");
	}
}
