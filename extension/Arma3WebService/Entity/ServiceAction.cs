using System.Collections.Concurrent;
using System.Text.Json;
using Arma3WebService.Extensions;
using Arma3WebService.Models;
using Components.Entity;
using Microsoft.Net.Http.Headers;

namespace Arma3WebService.Entity;

public sealed class ServiceAction(
	ILogger<ServiceAction> logger,
	IDiscordBotService discordBotService)
{
	public async Task CallBackAction(IConnection session, Arma3PayloadCallBack command)
	{
		await session.SendArmaCallBackMessage(command);
	}
	public async Task TextAction(IConnection connection, Arma3PayloadText payload)
	{
		await connection.Send(payload.ToJsonString());
	}
	public async Task RptAction(IConnection connection, Arma3PayloadRPT payload)
	{
		logger.LogInformation("Receiving metaData for binary file '{Arma3PayloadRpt}'", payload);
		
		await using var fileStream = new FileStream(
			$".Rpt/{payload.FileName}", FileMode.Create, FileAccess.Write);
					
		await connection.ReceiveBinary(fileStream);
		logger.LogDebug("Stored binary file '{PayloadFileName}'", payload.FileName);
	}
	public async Task JsonStringAction(IConnection connection, Arma3PayloadJson payload)
	{
		logger.LogDebug("Received message '{PayloadJsonString}'", payload.JsonString);
		
		try
		{
			var deserialize = JsonSerializer.Deserialize(
				payload.JsonString,
				Arma3PayloadExtendedJsonSerializerContext.Default.Arma3PayloadExtended
			);
			
			if (deserialize == null) throw new NullReferenceException("JsonStringAction is Null.");
			await deserialize.Invoke(connection, discordBotService);
		}
		catch (Exception e)
		{
			logger.LogError(e, "JsonStringAction threw an exception...");
		}
	}
	
	
	public Task ArrayStringAction(IConnection connection, Arma3PayloadArrayString payload)
	{
		try
		{
			UpdateGameInfo(connection, payload.ArrayString);
			return Task.CompletedTask;
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}
	}
	
	private readonly List<string> ctxList = [];
	private readonly ConcurrentDictionary<string, Dictionary<string, string>?> _gameInfoConcurrentDictionary = [];
	
	private void UpdateGameInfo(IConnection connection, IEnumerable<string> infoString)
	{
		if (ctxList.Count == 0) return;

		try
		{
			var collection = infoString
				.Select(x =>
					JsonSerializer.Deserialize<List<string>>(x)
				)
				.ToDictionary(
					value => value![0],
					value => value![1]
				);
			
			var identity = connection.websocketContext.GetIndentity();
			logger.LogInformation("\"{identity}\" received game info", identity);

			_gameInfoConcurrentDictionary[identity] = collection;
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
	public async Task SSE_Logging(HttpContext ctx, string sessionIdentity)
	{
		var ctxID = ctx.TraceIdentifier;
		ctxList.Add(ctxID);

		ctx.Response.Headers.Append(HeaderNames.ContentType, "text/event-stream");
		while (!ctx.RequestAborted.IsCancellationRequested)
		{
			await Task.Delay(1000);
			if (!_gameInfoConcurrentDictionary.TryGetValue(sessionIdentity, out var logItem)) continue;
			if (logItem is null) continue;
			
			await JsonSerializer.SerializeAsync(ctx.Response.Body, logItem);
			await ctx.Response.WriteAsync("\n\n");
			await ctx.Response.Body.FlushAsync();
			_gameInfoConcurrentDictionary[sessionIdentity] = null;
		}

		ctxList.Remove(ctxID);
	}
}
