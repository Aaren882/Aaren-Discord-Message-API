using System.Text.Json;
using System.Threading.Tasks.Dataflow;
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
	private readonly Task _socketWorker;
	private readonly ActionBlock<WebSocketWorkingTask> _websocketWorker;

	public WebSocketLocalWorker()
	{
		_websocketWorker = new ActionBlock<WebSocketWorkingTask>(async task =>
		{
			try
			{
				Logger(null, $"{nameof(WebSocketLocalWorker)} Run => {task.headerObj}");
				await task.Run(); // 執行您的任務
			}
			catch (Exception e)
			{
				Logger(e, nameof(WebSocketLocalWorker));
			}
		}, new ExecutionDataflowBlockOptions
		{
			MaxDegreeOfParallelism = 1 
		});
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
		_websocketWorker.Post(webSocketTask);
	}
}
