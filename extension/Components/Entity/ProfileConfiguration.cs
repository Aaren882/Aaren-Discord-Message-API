using System.Text.Json.Serialization;

namespace Components.Entity;

public enum ProfileConfigurationType
{
	Configuration,
	ConfigurationDateOffsets
}

public readonly record struct ProfileConfiguration(
	Arma3ClientProfileConfiguration Configuration,
	string? MessageId = null,
	string? RPT_Directory = null
)
{
	private const string Directory = "profiles";
	public ProfileConfigurationDateOffsets GetDateOffsets()
	{
		var paths = Configuration.GetTemplateFileList();
		long[] dateOffSets = [..paths.Select(path =>
		{
			FileInfo fileInfo = new(Path.Combine(Directory, path + ".json"));
			return ((DateTimeOffset)fileInfo.LastWriteTime).ToUnixTimeSeconds();
		})];

		return new ProfileConfigurationDateOffsets(MessageId, dateOffSets, Configuration);
	}
};

public readonly record struct ProfileConfigurationDateOffsets(
	string? MessageId,
	long[] ProfileDateOffsets,
	Arma3ClientProfileConfiguration Configuration
);

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(ProfileConfiguration))]
public sealed partial class ProfileConfigurationJsonSerializerContext : JsonSerializerContext;
