using System.Collections.Concurrent;
using System.Text.Json;
using Components.Entity;
using static ServiceConnection.ServiceStartup;
namespace ServiceConnection.WebService;

public sealed class ServiceRequestHandler
{
	private ConcurrentDictionary<Arma3PayloadServiceRequest, Task> _requestHandler = new(); 
	internal static void RespondRequest(Arma3PayloadServiceRequest request, string handShakePayload)
	{
		var serviceInteractions = ServiceStartup.serviceInteractions;
		if (serviceInteractions is null) return;
		
		serviceInteractions.WebSocketTrafficWriter(
			GetRespond(request, handShakePayload)
		);
	}

	private static async Task GetRespond(Arma3PayloadServiceRequest request, string handShakePayload)
	{
		var type = request.ActionType;
		
		//- which action should do
		Task task = null;
		switch (type)
		{
			case 1: //- Send Rpt lines
				task = RespondWebSocketPrintRpt(RptFileDirectory, 50);
				break;
			case 2: //- RequestRpt
				var fileInfo = new FileInfo(RptFileDirectory);
				var totalChunks = (int)Math.Ceiling((double)fileInfo.Length / (64 * 1024));

				// Send Metadata (as text message)
				var metadata = new Arma3PayloadBinary
				(
					fileInfo.Name,
					fileInfo.Length,
					fileInfo.CreationTime,
					totalChunks,
					".temp"
				);
				
				handShakePayload = JsonSerializer.Serialize(
					request with { Payload = metadata },
					Arma3PayloadJsonSerializerContext.Default.Arma3Payload
				);
				
				task = RespondWebSocketExportRpt(RptFileDirectory, metadata);
				break;
		}
		
		//- Put respond into websocket queue first
		await serviceInteractions.SendWebSocketMessage(handShakePayload);
		await task;
	}

	private static async Task RespondWebSocketPrintRpt(string filePath, int linesCount)
	{
		await serviceInteractions!.WsClient.SendRptLinesAsync(filePath, linesCount);
	}
	private static async Task RespondWebSocketExportRpt(string filePath, Arma3PayloadBinary metadata)
	{
		try
		{
			await serviceInteractions.WsClient.SendBinaryAsync(filePath, metadata);
		}
		catch (Exception e)
		{
			Logger(e, "");
		}
	}
}
