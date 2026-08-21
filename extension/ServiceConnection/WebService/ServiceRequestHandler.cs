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
		ArgumentNullException.ThrowIfNull(RptFileDirectory);
		FileInfo RPTFileInfo = new(RptFileDirectory);

		//- which action should do
		Func<ValueTask>? task = null;
		switch (request.ActionType)
		{
			case 1: //- Send Rpt lines
				Arma3PayloadBinary RptLineMetaData = new
				(
					RPTFileInfo.Name,
					RPTFileInfo.Length,
					RPTFileInfo.CreationTime
				);
				request = request with { Payload = RptLineMetaData };
				task = () => serviceInteractions.WsClient.SendRptLinesAsync(serviceInteractions!.AccessName, RptFileDirectory, RptLineMetaData, 50);
				break;
			case 2: //- RequestRpt
				const int chunkSize = 60 * 1024;
				var totalChunks = (int)Math.Ceiling((double)RPTFileInfo.Length / chunkSize);

				// Send Metadata (as text message)
				Arma3PayloadBinary BinaryMetaData = new
				(
					RPTFileInfo.Name,
					RPTFileInfo.Length,
					RPTFileInfo.CreationTime,
					totalChunks,
					null
				);

				request = request with { Payload = BinaryMetaData };
				task = () => serviceInteractions.WsClient.SendBinaryAsync(serviceInteractions!.AccessName, RptFileDirectory, BinaryMetaData, chunkSize);
				break;
		}
		ArgumentNullException.ThrowIfNull(task);

		//- Put respond into websocket queue first
		Logger(null, $"{nameof(ServiceRequestHandler)}.{nameof(GetRespond)} : \nrequest = {request}");

		//- Send MetaData
		var payload = JsonSerializer.SerializeToUtf8Bytes(
			request,
			Arma3PayloadJsonSerializerContext.Default.Arma3Payload
		)!;
		await serviceInteractions.WsClient.SendAsync(payload, WebSocketMessageType.Binary, true);
		await task.Invoke();
	}
}
