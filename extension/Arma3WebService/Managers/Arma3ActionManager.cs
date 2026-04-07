using System.Text.Json;
using Arma3WebService.Entity;
using Components.Entity;

namespace Arma3WebService.Managers;

public interface IArma3ActionManager
{
	Task GetAction(IConnection connection, Arma3Payload payload);
}


public class Arma3ActionManager(ServiceAction serviceAction) : IArma3ActionManager
{
	public Task GetAction(IConnection connection, Arma3Payload payload)
	{
		return (payload.Type) switch
		{
			Arma3PayLoadType.Text => TextAction(connection, payload as Arma3PayloadText),
			Arma3PayLoadType.ArrayString => ArrayStringAction(connection, payload as Arma3PayloadArrayString),
			Arma3PayLoadType.JsonString => JsonStringAction(connection, payload as Arma3PayloadJson),
			Arma3PayLoadType.Rpt => RptAction(connection, payload as Arma3PayloadRPT)
		};
	}
	
	private async Task TextAction(IConnection connection, Arma3PayloadText payload)
	{
		var json = JsonSerializer.Serialize(payload, Arma3PayloadJsonSerializerContext.Default.Arma3PayloadText);
		await connection.Send(json);
	}
	private async Task RptAction(IConnection connection, Arma3PayloadRPT payload)
	{
		Console.WriteLine($"Receiving metaData for binary file '{payload}'");
		
		await using var fileStream = new FileStream(
			payload.FileName, FileMode.Create, FileAccess.Write);
					
		await connection.ReceiveBinary(fileStream);
		Console.WriteLine($"Stored binary file '{payload.FileName}'");
	}
	private Task CallBackAction(IConnection connection, Arma3PayloadCallBack payload)
	{
		throw new NotImplementedException();
	}
	private async Task JsonStringAction(IConnection connection, Arma3PayloadJson payload)
	{
		Console.WriteLine($"Received message '{payload.JsonString}'");
		
		var dto = JsonSerializer.Deserialize(payload.JsonString, MsgPayload_JsonContext.Default.DiscordMessageDto);
		if (dto != null) await serviceAction.InvokeDiscordBotMessage(dto);
	}
	private async Task ArrayStringAction(IConnection connection, Arma3PayloadArrayString payload)
	{
		try
		{
			var collection = payload?.ArrayString
				.Select(x =>
					JsonSerializer.Deserialize<List<string>>(x)
				)
				.ToDictionary(
					value => value![0],
					value => value![1]
				);
					
			Console.WriteLine(collection);
		}
		catch (ArgumentException e)
		{
			Console.WriteLine(e);
		}
	}
}
