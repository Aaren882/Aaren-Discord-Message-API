using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ZLogger;

namespace ExtensionComponents.Tools;

public static class LoggerBase// : ILoggerInternal
{
	private const int LogLimitCount = 10;
	private static readonly string ExtFilePath = Util.AssemblyPath!;
	private static readonly string LogFilePath = Path.Combine(ExtFilePath, "logs");
	private static readonly string LogFileName = Path.Combine(
		LogFilePath,
		$"{DateTime.Now:yyyy-MM-dd.HH-mm-ss}.log");
	private readonly static ILogger SystemLogger = LoggerFactory.Create(builder => builder.UseDefaultFileLogger()).CreateLogger("SYSTEM");
	public static ILoggingBuilder UseDefaultFileLogger(this ILoggingBuilder Builder)
	{
		Builder.SetMinimumLevel(LogLevel.Trace);
		Builder.AddZLoggerFile(
			LogFileName,
			options =>
			{
				options.FileShared = true;
				options.UsePlainTextFormatter(formatter =>
				{
					formatter.SetPrefixFormatter($"{0} ({2}) [{1:short}] ", (in MessageTemplate template, in LogInfo info) => template.Format($"{info.Timestamp.Local.DateTime:HH-mm-ss}", info.LogLevel, info.Category));
					formatter.SetExceptionFormatter((writer, ex) => Utf8StringInterpolation.Utf8String.Format(writer, $"{ex.Message}"));
				});
			}
		);

		return Builder;
	}

	public static void SetupFileLogger(this ServiceCollection services)
	{
		services.AddLogging(Builder =>
		{
			Builder.ClearProviders();
			Builder.UseDefaultFileLogger();
		});
		CleanLogs();
	}
	/* private record LoggerObject(Exception? e, string s)
	{
		public void WriteLog()
		{
			var s = this.s;
			if (!Directory.Exists(ExtFilePath))
				Directory.CreateDirectory(ExtFilePath);
			if (!Directory.Exists(LogFilePath))
				Directory.CreateDirectory(LogFilePath);

			using StreamWriter file = new(LogFileName, true);
			if (string.IsNullOrEmpty(s))
				s = e!.Message;

			if (s.Length > 0)
				file.WriteLine($"{DateTime.Now:T} - {s}");
		}
	};
	private static readonly ActionBlock<LoggerObject> _loggerProcess = new
	(
		o => o.WriteLog(),
		new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 }
	); */

	public static void Trace(string Name, string content)
	{
#if DEBUG
		SystemLogger.LogTrace("({Name}) => {content}", Name, content);
#endif
	}

	public static void Log(Exception? e, string s = "")
	{
		// _loggerProcess.Post(new(e, s));
		if (e is null)
			SystemLogger.LogInformation("{Log}", s);
		else
			SystemLogger.LogError(e, "{Log}", s);
	}

	public static void CleanLogs()
	{
		var files = Directory.GetFiles(LogFilePath);

		//- Check how many logs
		Trace(nameof(CleanLogs), "Checking how many logs...");
		if (files.Length < LogLimitCount) return;

		Dictionary<string, DateTime> dict = [];
		foreach (var file in files)
		{
			var time = Directory.GetCreationTime(file);
			dict.Add(file, time);
		}

		var list = dict.OrderBy(x => x.Value).ToList();
		for (var i = 0; i < list.Count - LogLimitCount; i++)
		{
			var logFile = list[i].Key;
			File.Delete(logFile);
		}

		Trace(nameof(CleanLogs), "Old logs are cleaned out.");
	}
}
