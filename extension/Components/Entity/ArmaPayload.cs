using System.Text;
using System.Text.Json.Serialization;

namespace Components.Entity;

public enum Arma3PayLoadType
{
	Text = 1,
	Rpt = 2, //- in game *.rpt logs
	Command = 3,
	GameInfo = 4,
	JsonString = 5,
	ArrayString = 6,
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(Arma3PayloadText), (int)Arma3PayLoadType.Text)]
[JsonDerivedType(typeof(Arma3PayloadRPT), (int)Arma3PayLoadType.Rpt)]
[JsonDerivedType(typeof(Arma3PayloadCallBack), (int)Arma3PayLoadType.Command)]
[JsonDerivedType(typeof(Arma3PayloadJson), (int)Arma3PayLoadType.JsonString)]
[JsonDerivedType(typeof(Arma3PayloadArrayString), (int)Arma3PayLoadType.ArrayString)]
public abstract record Arma3Payload
{
	public abstract Arma3PayLoadType Type { get; }
	public static DateTime Timestamp => DateTime.Now;
}

public record Arma3PayloadJson
(
	int ProcessType,
	string JsonString
) : Arma3Payload
{
	[JsonIgnore]
	public override Arma3PayLoadType Type => Arma3PayLoadType.JsonString;
};

public record Arma3PayloadRPT
(
	string FileName,
	long FileSize,
	DateTime CreatedTime,
	int TotalChunks
) : Arma3Payload
{
	[JsonIgnore]
	public override Arma3PayLoadType Type => Arma3PayLoadType.Rpt;
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

public record Arma3PayloadArrayString
(
	IEnumerable<string> ArrayString
) : Arma3Payload
{
	[JsonIgnore]
	public override Arma3PayLoadType Type => Arma3PayLoadType.ArrayString;
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
