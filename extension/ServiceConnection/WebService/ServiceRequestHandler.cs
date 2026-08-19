using System.Net.WebSockets;
using System.Text.Json;
using Components.Entity;
using static ExtensionComponents.ExtensionStartup;
using static ServiceConnection.ServiceStartup;

namespace ServiceConnection.WebService;

public sealed class ServiceRequestHandler
{
	// private ConcurrentDictionary<Arma3PayloadServiceRequest, Task> _requestHandler = new(); 
	internal async ValueTask RespondRequest(Arma3PayloadServiceRequest request)
	{
		var serviceInteractions = ServiceStartup.serviceInteractions;
		ArgumentNullException.ThrowIfNull(serviceInteractions);
		await GetRespond(request);
	}

	private async ValueTask GetRespond(Arma3PayloadServiceRequest request)
	{
		ArgumentNullException.ThrowIfNull(serviceInteractions);

		//- which action should do
		switch (request.ActionType)
		{
			case 1: //- Send Rpt lines
				ArgumentNullException.ThrowIfNull(RptFileDirectory);
				await RespondWebSocketPrintRpt(RptFileDirectory, 50);
				break;
			case 2: //- RequestRpt
				ArgumentNullException.ThrowIfNull(RptFileDirectory);
				const int chunkSize = 60 * 1024;
				var fileInfo = new FileInfo(RptFileDirectory);
				var totalChunks = (int)Math.Ceiling((double)fileInfo.Length / chunkSize);

				// Send Metadata (as text message)
				Arma3PayloadBinary metadata = new
				(
					fileInfo.Name,
					fileInfo.Length,
					fileInfo.CreationTime,
					totalChunks,
					null
				);

				request = request with { Payload = metadata };

				//- Send MetaData
				var payload = JsonSerializer.SerializeToUtf8Bytes(
					request,
					Arma3PayloadJsonSerializerContext.Default.Arma3Payload
				)!;
				await serviceInteractions.WsClient.SendAsync(payload, WebSocketMessageType.Binary, true);
				await serviceInteractions.WsClient.SendBinaryAsync(serviceInteractions!.AccessName, RptFileDirectory, metadata, chunkSize);
				// await RespondWebSocketExportRpt(RptFileDirectory, metadata);
				break;
		}

		//- Put respond into websocket queue first
		Logger(null, $"{nameof(ServiceRequestHandler)}.{nameof(GetRespond)} : \nrequest = {request}");
		/* serviceInteractions?.SocketLocalWorker.WebSocketTrafficWriter(
			request,
			() => task!
		); */
	}

	private async ValueTask RespondWebSocketPrintRpt(string filePath, int linesCount)
	{
		// await serviceInteractions?.WsClient.SendRptLinesAsync(filePath, linesCount)!;
	}
	private async ValueTask RespondWebSocketExportRpt(string filePath, Arma3PayloadBinary metadata)
	{
		// return serviceInteractions!.WsClient.SendBinaryAsync(filePath, metadata);
	}
}
