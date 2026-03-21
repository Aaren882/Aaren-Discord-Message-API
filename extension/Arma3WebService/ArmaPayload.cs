using System.Text;
using System.Text.Json.Serialization;

namespace Arma3WebService;

public enum Arma3PayLoadType
{
	Message = 1,
	Rpt = 2, //- in game *.rpt logs
	Command = 3,
}
public record struct Arma3Payload(
	Arma3PayLoadType MessageType
)
{
	public Arma3PayloadMessage Message { get; init; }
	public Arma3PayloadRPT Rpt { get; init; }
	public Arma3PayloadCallBack CallBack { get; init; }
	public DateTime Timestamp => DateTime.Now;
}

public abstract record IArma3Payload;

public record Arma3PayloadMessage(
	string Message
) : IArma3Payload;

public record Arma3PayloadRPT(
	string FileName,
	long FileSize,
	DateTime CreatedTime,
	int TotalChunks
) : IArma3Payload;

public record Arma3PayloadCallBack(
	string Function,
	string Data
) : IArma3Payload;

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
[JsonSerializable(typeof(Arma3Payload))]
[JsonSerializable(typeof(Arma3PayloadRPT))]
[JsonSerializable(typeof(Arma3PayloadCallBack))]
[JsonSerializable(typeof(List<Arma3Payload>))] // Add all root types used
[JsonSerializable(typeof(Arma3ServiceSecret))]
internal sealed partial class Arma3PayloadJsonSerializerContext : JsonSerializerContext;
