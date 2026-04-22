using System.Collections.Concurrent;
using System.Text.Json;
using Arma3WebService.DBContext;
using Arma3WebService.Extensions;
using Arma3WebService.Models;
using Components.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace Arma3WebService.Entity;

public sealed class ServiceAction(
	ILogger<ServiceAction> logger,
	IServiceProvider serviceProvider,
	IDiscordBotService discordBotService,
	IServiceScopeFactory ServiceScopeFactory
)
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
		logger.LogDebug("Received message \"{PayloadJsonString}\"", payload.JsonString);
		
		try
		{
			var deserialize = JsonSerializer.Deserialize(
				payload.JsonString,
				Arma3PayloadExtendedJsonSerializerContext.Default.Arma3PayloadExtended
			);
			
			if (deserialize == null) throw new NullReferenceException("JsonStringAction is Null.");
			await deserialize.Invoke(connection, serviceProvider);
		}
		catch (Exception e)
		{
			logger.LogError(e, "JsonStringAction threw an exception...");
		}
	}
	
	
	public async Task FlatJsonStringAction(IConnection connection, Arma3PayloadFlatJsonString payload)
	{
		var collection = payload.FlatJsonString;
		var identity = connection.websocketContext.GetIdentity();
		
		logger.LogDebug("\"{identity}\" received game info", identity);
		
		UpdateSSEGameInfo(identity, collection);
		await UpdateDiscordServerInfoMessageAsync(identity, collection);
	}
	
	private readonly List<string> ctxList = [];
	private readonly ConcurrentDictionary<string, Dictionary<string, string>?> _gameInfoSSEConcurrentDictionary = [];
	
	private void UpdateSSEGameInfo(string identity, Dictionary<string, string> collection)
	{
		if (ctxList.Count == 0) return;
		_gameInfoSSEConcurrentDictionary[identity] = collection;
	}
	private async Task UpdateDiscordServerInfoMessageAsync(string sessionIdentity, Dictionary<string, string> logItem)
	{
		using var scope = ServiceScopeFactory.CreateScope();
		await using var dbContext = scope.ServiceProvider.GetRequiredService<ServiceDbContext>();
		
		var serverIdentity = await dbContext.Identifier.FirstOrDefaultAsync(o => o.profileName == sessionIdentity);
		
		//- If messageId not set  
		if (serverIdentity is null)
		{
			logger.LogError("\"{sessionIdentity}\" does not exist.",sessionIdentity);
			return;
		}
		if (serverIdentity.messageId is 0) return;
		
		var serverInfo = dbContext.UpdateServerInfo.FirstOrDefault(o => o.messageId == serverIdentity.messageId);
		if (serverInfo is null) return;
		
		var infoMessage = await File.ReadAllTextAsync(serverInfo.filePath);
		infoMessage = logItem.Aggregate(
			infoMessage,
			(current, item) => current.Replace(item.Key, item.Value)
		);

		var payload = JsonSerializer.Deserialize(
			infoMessage,
			MsgPayload_JsonContext.Default.DiscordMessageDto
		);
			
		await discordBotService.ModifyMessageAsync(serverIdentity.messageId, payload!);
	}
	public async Task SSE_Logging(HttpContext ctx, string sessionIdentity)
	{
		var ctxID = ctx.TraceIdentifier;
		ctxList.Add(ctxID);

		ctx.Response.Headers.Append(HeaderNames.ContentType, "text/event-stream");
		while (!ctx.RequestAborted.IsCancellationRequested)
		{
			await Task.Delay(1000);
			if (!_gameInfoSSEConcurrentDictionary.TryGetValue(sessionIdentity, out var logItem)) continue;
			if (logItem is null) continue;
			
			await JsonSerializer.SerializeAsync(ctx.Response.Body, logItem);
			await ctx.Response.WriteAsync("\n\n");
			await ctx.Response.Body.FlushAsync();
			_gameInfoSSEConcurrentDictionary[sessionIdentity] = null;
		}

		ctxList.Remove(ctxID);
	}
}
