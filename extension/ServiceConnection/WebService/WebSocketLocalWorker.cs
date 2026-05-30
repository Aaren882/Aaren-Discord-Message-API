using System.Collections.Concurrent;
using System.Text.Json;
using Components.Entity;
using static ServiceConnection.ServiceStartup;

namespace ServiceConnection.WebService;

public readonly record struct WebSocketWorkingTask(Task? headerTask, Task webSocketTask)
{
	public async Task Run()
	{
		var hasHeader = headerTask is not null;
		try
		{
			if (hasHeader) await headerTask!;
			await webSocketTask;
		}
		catch (Exception e)
		{
			Logger(e, "[WebSocketWorkingTask]");
			
			//- If Header exist and exception is thrown
			if (!hasHeader) return;
			
			await serviceInteractions?.SendWebSocketMessage(e.ToString())!;
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
	
	public void WebSocketTrafficWriter(Task webSocketTask)
		=> WebSocketTrafficWriter((Task?)null, webSocketTask);
	
	public void WebSocketTrafficWriter(Arma3Payload header, Task webSocketTask)
	{
		var json = JsonSerializer.Serialize(header, Arma3PayloadJsonSerializerContext.Default.Arma3Payload);
		WebSocketTrafficWriter(json, webSocketTask);
	}
	public void WebSocketTrafficWriter(string header, Task webSocketTask)
	{
		WebSocketTrafficWriter(serviceInteractions.SendWebSocketMessage(header), webSocketTask);
	}
	
	public void WebSocketTrafficWriter(Task? headerTask, Task webSocketTask)
	{
		EnqueueTask(new WebSocketWorkingTask(headerTask, webSocketTask));
	}
	
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
				Logger(e, "");
			}
		}
	}
}
