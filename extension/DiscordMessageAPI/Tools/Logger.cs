namespace DiscordMessageAPI.Tools
{
	internal class Logger
	{
		private static readonly string ExtFilePath = Util.AssemblyPath!;
		private static readonly string LogFilePath = Path.Combine(ExtFilePath, "logs");
		private static readonly string LogFileName = Path.Combine(
			LogFilePath,
			$"{DateTime.Now.ToString("yyyy-MM-dd.HH-mm-ss")}.DiscordMessageAPI.log");

		/// <summary>
		/// Writes a trace message to the logger if debugging is enabled.
		/// </summary>
		/// <remarks>No output is generated if debugging is not enabled. This method is intended for
		/// internal diagnostic purposes.</remarks>
		/// <param name="Name">The name or category associated with the trace message. Used to identify the source or context of the trace
		/// output.</param>
		/// <param name="content">The content of the trace message to be logged.</param>
		internal static void Trace(string Name, string content)
		{
#if DEBUG
			Log(null, $"TRACER - {Name} : {content}");
#endif
		}
		internal static void Log(Exception? e, string s = "", bool loop = false)
		{
			try
			{
				if (!Directory.Exists(ExtFilePath))
					Directory.CreateDirectory(ExtFilePath);
				if (!Directory.Exists(LogFilePath))
					Directory.CreateDirectory(LogFilePath);

				using (StreamWriter file = new StreamWriter(LogFileName, true))
				{
					if (string.IsNullOrEmpty(s))
						s = e!.Message;
					if (s.Length > 0)
						file.WriteLine($"{DateTime.Now.ToString("T")} - {s}");
				}
			}
			catch (Exception i)
			{
				if (!loop)
					Log(i, null, true);
			}
		}

		internal static void CleanLogs()
		{
			var limit = 10;
			var files = Directory.GetFiles(LogFilePath);

			//- Check how many logs
			if (files.Length < limit) return;

			Dictionary<string, DateTime> dict = new();
			foreach (var file in files)
			{
				var time = Directory.GetCreationTime(file);
				dict.Add(file, time);
			}

			var list = dict.OrderByDescending(x => x.Value).ToList();
			for (int i = 0; i < list.Count - limit; i++)
			{
				var logFile= list[i].Key;
				File.Delete(logFile);
			}
		}
	}
}
