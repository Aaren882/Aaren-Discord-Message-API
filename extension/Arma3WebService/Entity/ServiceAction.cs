using System.Text.Json;
using Arma3WebService.Models;
using Components.Entity;

namespace Arma3WebService.Entity;

public sealed class ServiceAction(
	ILogger<ServiceAction> logger,
	IDiscordBotService discordBotService
)
{
	
	public async Task InvokeArmaCallBack(IConnection session, Arma3PayloadCallBack command)
	{
		await session.SendArmaCallback(command);
	}
		
	public async Task InvokeDiscordBotMessage(DiscordMessageDto message)
	{
		await discordBotService?.SendMessageAsync(message)!;
	}
	
	public async Task TextAction(IConnection connection, Arma3PayloadText payload)
	{
		var json = JsonSerializer.Serialize(payload, Arma3PayloadJsonSerializerContext.Default.Arma3PayloadText);
		await connection.Send(json);
	}
	public async Task RptAction(IConnection connection, Arma3PayloadRPT payload)
	{
		logger.LogInformation("Receiving metaData for binary file '{Arma3PayloadRpt}'", payload);
		
		await using var fileStream = new FileStream(
			payload.FileName, FileMode.Create, FileAccess.Write);
					
		await connection.ReceiveBinary(fileStream);
		logger.LogInformation("Stored binary file '{PayloadFileName}'", payload.FileName);
	}
	public Task CallBackAction(IConnection connection, Arma3PayloadCallBack payload)
	{
		throw new NotImplementedException();
	}
	public async Task JsonStringAction(IConnection connection, Arma3PayloadJson payload)
	{
		logger.LogInformation("Received message '{PayloadJsonString}'", payload.JsonString);
		
		var dto = JsonSerializer.Deserialize(payload.JsonString, MsgPayload_JsonContext.Default.DiscordMessageDto);
		if (dto != null) await InvokeDiscordBotMessage(dto);
	}
	
	private Queue<Dictionary<string,string>> logQueue = new ();
	private bool _avalibleSseLogging = false;
	public async Task ArrayStringAction(IConnection connection, Arma3PayloadArrayString payload)
	{
		if (!_avalibleSseLogging) return;
		
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
			logger.LogInformation("{collection}", collection);
			
			logQueue.Enqueue(collection);
		}
		catch (ArgumentNullException)
		{
			logger.LogError("Source or keySelector or elementSelector is null. -or- keySelector produces a key that is null.");
		}
		catch (ArgumentException)
		{
			logger.LogError("The payload has duplicated keys");
		}
		catch (Exception e)
		{
			logger.LogError(e, "An exception occurred");
		}
	}

	public async Task SSE_Logging(HttpContext ctx)
	{
		_avalibleSseLogging = true;

		do
		{
			if (logQueue.TryDequeue(out var logItems))
			{
				// var items = logItems
				// 	.Select<KeyValuePair<string, string>, string[]>(x => [x.Key, x.Value])
				// 	.ToArray();
				
				foreach (var item in logItems)
				{
					await ctx.Response.WriteAsync($"data: ");
					await JsonSerializer.SerializeAsync(ctx.Response.Body, new {
						item.Key,
						item.Value
					});
					await ctx.Response.WriteAsync($"\n\n");
					await ctx.Response.Body.FlushAsync();
				}
			}
			else
			{
				await ctx.Response.WriteAsync("");
				await ctx.Response.Body.FlushAsync();
			}
			
			// some artificial delay to not overwhelm the client
			await Task.Delay(1000);
		} while (!ctx.RequestAborted.IsCancellationRequested);
		
		_avalibleSseLogging = false; 
	}
}
