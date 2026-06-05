using System.Text.Json;
using Components.Entity;
using ExtensionComponents.Entity;
using static ServiceConnection.ServiceStartup;
using static ExtensionComponents.ExtensionStartup;
using static ExtensionComponents.Tools.Util;

namespace ServiceConnection.Tools;

public static class Util
{
	public static IEnumerable<FileInfo> GetDirectoryFiles(string path)
	{
		var combined= Path.Combine(AssemblyPath, path);

		return Directory.GetFiles(combined)
			.Select(x => new FileInfo(x));
	}

	public static IEnumerable<FileInfo> GetFilesFileInfos(IEnumerable<string> paths)
	{
		return paths.Select(path =>
		{
			var fileInfo = new FileInfo(Path.Combine(AssemblyPath, path));
			return fileInfo.Exists ? 
				fileInfo : 
				throw new FileNotFoundException($"File \"{path}\" not found");
		});
	}

	public static List<string> GetDirectoryFileNames(string path)
		=> GetDirectoryFiles(path).Select(x => x.Name).ToList();

	public static string GetLatestFile(string path)
	{
		var fileInfo = GetDirectoryFiles(path)
			.Where(x => x.Exists)
			.MaxBy(x => x.CreationTime);

		return fileInfo?.FullName ?? throw new NullReferenceException($"No file exist in : {path}");
	}
	public static string GetClosestDateFile(string path, DateTimeOffset dateTimeOffset)
	{
		var fileInfo = GetDirectoryFiles(path)
			.MinBy(x =>
				dateTimeOffset - x.CreationTime
			);

		return fileInfo?.FullName ?? throw new NullReferenceException($"No file exist in : {path}");
	}
	public static string GetCurrentRpt()
	{
		var path = serviceInteractions?.RPTDirectory;
		var dateTimeOffset = ExtensionInitTime;

		Tracer(nameof(GetCurrentRpt), $"RPTDirectory : {path}, StartTimeOffset : {dateTimeOffset:F}");
		
		var fileInfo = GetDirectoryFiles(path)
			.Where(
				x => x.Extension == ".rpt" && ExtensionInitTime > x.CreationTime
			)
			.MaxBy(x => x.CreationTime);

		return fileInfo?.FullName ?? throw new NullReferenceException($"No file exist in : {path}");
		
	}

	public static int CallExtensionCallback(ExtensionCallback extensionCallback, Arma3Payload payload)
	{
		var data = JsonSerializer.Serialize(payload, Arma3PayloadJsonSerializerContext.Default.Arma3Payload);
		return extensionCallback("DISCORD_API", ((int)payload.Type).ToString(), data);
	}
}
