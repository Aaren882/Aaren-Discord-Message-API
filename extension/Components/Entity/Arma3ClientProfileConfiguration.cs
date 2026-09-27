using System.Text.Json.Serialization;

namespace Components.Entity;

public readonly record struct Arma3ClientProfileConfiguration(
	string MessageTemplate = ".profile/MessageTemplate/default.json",
	string MessageOfflineTemplate = ".profile/MessageOfflineTemplate/default.json",
	string? MessageActions = null
)
{
	public string[] GetTemplateFileList(string prefix = "")
	{
		string[] fileInfos;
		if (MessageActions == null)
		{
			fileInfos = [MessageTemplate, MessageOfflineTemplate];
		}
		else
		{
			fileInfos = [MessageTemplate, MessageOfflineTemplate, MessageActions];
		}

		return [.. fileInfos.Select(x => Path.Combine(prefix, x))];
	}
	public Arma3PayloadBinary[] ToPayloadBinaryList(string prefix = "")
	{
		return [..GetTemplateFileList(prefix).Select(x =>
				{
					FileInfo _fileInfo = new(x);
					Arma3PayloadBinary payload = new(
						_fileInfo.Name,
						_fileInfo.Length,
						_fileInfo.CreationTime
					);
					return payload;
				})];
	}
};

public enum DBConfigType
{
	UpdateAndSaveProfile
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
[JsonDerivedType(typeof(UpdateAndSaveProfile), (int)DBConfigType.UpdateAndSaveProfile)]
public abstract record DBConfigAction
{
	[JsonIgnore]
	public abstract DBConfigType Type { get; }
};

public record UpdateAndSaveProfile(
	Arma3PayloadBinary[] MetaDataList,
	Arma3ClientProfileConfiguration Configuration
) : DBConfigAction
{
	[JsonIgnore]
	public override DBConfigType Type => DBConfigType.UpdateAndSaveProfile;
};
