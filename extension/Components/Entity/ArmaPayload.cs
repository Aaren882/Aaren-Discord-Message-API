using System.Text;
using System.Text.Json.Serialization;

namespace Components.Entity;

public enum Arma3PayLoadType
{
	Text = 1,
	Binary = 2,
	Command = 3,
	RptLine = 4,
	JsonString = 5,
	FlatJsonString = 6,
	ServiceRequest = 7,
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Arma3PayloadText), (int)Arma3PayLoadType.Text)]
[JsonDerivedType(typeof(Arma3PayloadBinary), (int)Arma3PayLoadType.Binary)]
[JsonDerivedType(typeof(Arma3PayloadCallBack), (int)Arma3PayLoadType.Command)]
[JsonDerivedType(typeof(Arma3PayloadRptLine), (int)Arma3PayLoadType.RptLine)]
[JsonDerivedType(typeof(Arma3PayloadJson), (int)Arma3PayLoadType.JsonString)]
[JsonDerivedType(typeof(Arma3PayloadFlatJsonString), (int)Arma3PayLoadType.FlatJsonString)]
[JsonDerivedType(typeof(Arma3PayloadServiceRequest), (int)Arma3PayLoadType.ServiceRequest)]
public abstract record Arma3Payload
{
	public abstract Arma3PayLoadType Type { get; }
	public static DateTime Timestamp => DateTime.Now;
}

public record Arma3PayloadJson
(
	string JsonString
) : Arma3Payload
{
	[JsonIgnore]
	public override Arma3PayLoadType Type => Arma3PayLoadType.JsonString;
};

public record Arma3PayloadBinary
(
	string FileName,
	long FileSize,
	DateTime CreatedTime,
	int TotalChunks,
	string? DirectoryPrefix
) : Arma3Payload
{
	[JsonIgnore]
	public override Arma3PayLoadType Type => Arma3PayLoadType.Binary;
};

public record Arma3PayloadRptLine
(
	string FileName,
	DateTime CreatedTime
) : Arma3Payload
{
	[JsonIgnore]
	public override Arma3PayLoadType Type => Arma3PayLoadType.RptLine;
};

public record Arma3PayloadCallBack(
	string Function,
	string Data
) : Arma3Payload
{
	[JsonIgnore]
	public override Arma3PayLoadType Type => Arma3PayLoadType.Command;
};

public record Arma3PayloadText(
	string Message
) : Arma3Payload
{
	[JsonIgnore]
	public override Arma3PayLoadType Type => Arma3PayLoadType.Text;
};

public record Arma3PayloadFlatJsonString
(
	Dictionary<string, string> FlatJsonString
) : Arma3Payload
{
	[JsonIgnore]
	public override Arma3PayLoadType Type => Arma3PayLoadType.FlatJsonString;
};

public record Arma3PayloadServiceRequest
(
	ushort ActionType,
	string RequestGuildId,
	Arma3Payload? Payload = null
) : Arma3Payload
{
	[JsonIgnore]
	public override Arma3PayLoadType Type => Arma3PayLoadType.ServiceRequest;
};

//- Service
public record struct ServiceAuthenticationHeader(
	string Username,
	string Password
)
{
	public override string ToString()
	{
		var usernamePassword = string.Join(':', [Username, Password]);
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(usernamePassword));
	}
};
public record Arma3ServiceSecret(
	string ServiceUri,
	string WebSocketServiceUri,
	string RPT_Directory,
	ServiceAuthenticationHeader Secret
);

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)] // Optional: Add desired options
[
	JsonSerializable(typeof(Arma3Payload)),
	JsonSerializable(typeof(Arma3ServiceSecret))
]
public sealed partial class Arma3PayloadJsonSerializerContext : JsonSerializerContext;
