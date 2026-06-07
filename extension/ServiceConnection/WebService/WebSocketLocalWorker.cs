using System.Collections.Concurrent;
using System.Text.Json;
using Components.Entity;
using static ExtensionComponents.ExtensionStartup;
using static ServiceConnection.ServiceStartup;

namespace ServiceConnection.WebService;

public record WebSocketWorkingTask(Arma3Payload? headerObj, Func<Task> webSocketTask)
{
	public async Task Run()
	{
		var hasHeader = headerObj is not null;
		try
		{
			if (hasHeader)
			{
				var json = JsonSerializer.Serialize(headerObj, Arma3PayloadJsonSerializerContext.Default.Arma3Payload);
				await serviceInteractions!.SendWebSocketMessageAsync(json);
			}
			await webSocketTask.Invoke();
		}
		catch (Exception e)
		{
			Logger(e, "[WebSocketWorkingTask]");
			
			//- If Header exist and exception is thrown
			if (!hasHeader) return;
			
			await serviceInteractions?.SendWebSocketMessageAsync(e.ToString())!;
		}
	}
}

public sealed class WebSocketLocalWorker
{
	private readonly ConcurrentQueue<WebSocketWorkingTask> _websocketWorkerQueue = new ();
	private readonly Task _socketWorker;

	public WebSocketLocalWorker()
	{
		_socketWorker = WebSocketTrafficReader();
	}
	
	public void WebSocketTrafficWriter(Func<Task> webSocketTask)
		=> WebSocketTrafficWriter(null, webSocketTask);
	
	/*public async Task WebSocketTrafficWriter(Arma3Payload header, Task webSocketTask)
	{
		await WebSocketTrafficWriter(json, webSocketTask);
	}
	public Task WebSocketTrafficWriter(string header, Task webSocketTask)
		=> WebSocketTrafficWriter(
			serviceInteractions.SendWebSocketMessage(header),
			webSocketTask
		);*/
	
	public void WebSocketTrafficWriter(Arma3Payload? headerObj, Func<Task> webSocketTask)
		=> EnqueueTask(new WebSocketWorkingTask(headerObj, webSocketTask));
	
	private void EnqueueTask(WebSocketWorkingTask webSocketTask)
	{
		_websocketWorkerQueue.Enqueue(webSocketTask);
	}
	private async Task WebSocketTrafficReader()
	{
		Logger(null, "New WebSocketTrafficReader created.");
		while (true)
		{
			await Task.Delay(500);
			try
			{
				if (_websocketWorkerQueue.TryDequeue(out var task))
					await task.Run();
			}
			catch (Exception e)
			{
				Logger(e, nameof(WebSocketTrafficReader));
			}
		}
	}
}
