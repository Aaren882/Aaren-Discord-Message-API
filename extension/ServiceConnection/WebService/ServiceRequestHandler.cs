using System.Net.WebSockets;
using Components.Entity;
using static ExtensionComponents.ExtensionStartup;
using static ServiceConnection.ServiceStartup;

namespace ServiceConnection.WebService;

public sealed class ServiceRequestHandler
{
	internal async ValueTask RespondRequest(Arma3PayloadServiceRequest request)
	{
		var serviceInteractions = ServiceStartup.ServiceInteractions;
		ArgumentNullException.ThrowIfNull(serviceInteractions);
		await GetRespond(request);
	}

	private async ValueTask GetRespond(Arma3PayloadServiceRequest request)
	{
		var serviceInteractions = ServiceStartup.ServiceInteractions;
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

				// Send Metadata (as text message)
				Arma3PayloadBinary BinaryMetaData = new
				(
					RPTFileInfo.Name,
					RPTFileInfo.Length,
					RPTFileInfo.CreationTime
				);

				request = request with { Payload = BinaryMetaData };
				task = () => serviceInteractions.WsClient.SendBinaryAsync(serviceInteractions!.AccessName, RptFileDirectory, BinaryMetaData);
				break;
		}
		ArgumentNullException.ThrowIfNull(task);

		//- Put respond into websocket queue first
		Logger(null, $"{nameof(ServiceRequestHandler)}.{nameof(GetRespond)} : \nrequest = {request}");

		//- Send MetaData
		var payload = request.ToJsonBytes();
		await serviceInteractions.WsClient.SendAsync(payload, WebSocketMessageType.Text, true);
		await task.Invoke();
	}
}
