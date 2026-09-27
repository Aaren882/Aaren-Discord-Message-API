using System.Net.WebSockets;
using System.Threading.Channels;
using Components.Entity;
using Microsoft.Extensions.Logging;
using static ServiceConnection.ServiceStartup;

namespace ServiceConnection.WebService;

public sealed class ServiceRequestHandler
{
	private static readonly Channel<Arma3PayloadServiceRequest> channel = Channel.CreateUnbounded<Arma3PayloadServiceRequest>();
	private readonly ILogger logger;
	public ServiceRequestHandler(ILogger<ServiceRequestHandler> logger)
	{
		this.logger = logger;
		Task.Run(() => BackgroundAsync());
	}
	internal bool TryAddRequest(Arma3PayloadServiceRequest request)
	{
		ArgumentNullException.ThrowIfNull(ServiceStartup.ServiceInteractions);
		return channel.Writer.TryWrite(request);
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
			default:
				throw new ArgumentOutOfRangeException(nameof(request), $"Unsupported Request: {request}");
		}

		//- Put respond into websocket queue first
		logger.LogInformation("Processing request: {request}", request);

		//- Send MetaData
		var payload = request.ToJsonBytes();
		await serviceInteractions.WsClient.SendAsync(payload, WebSocketMessageType.Text, true);
		await task.Invoke();
	}
	private async Task BackgroundAsync()
	{
		logger.LogInformation("{Service} service started. HashCode : {HashCode}, Thread : {ThreadID}", nameof(ServiceRequestHandler), channel.GetHashCode(), Environment.CurrentManagedThreadId);
		await foreach (var request in channel.Reader.ReadAllAsync())
		{
			try
			{
				await GetRespond(request);
			}
			catch (ArgumentNullException ex)
			{
				logger.LogWarning(ex, "Request processing failed due to null argument.");
			}
			catch (ArgumentOutOfRangeException ex)
			{
				logger.LogWarning(ex, "Invalid request.");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "An error occurred while processing the request.");
			}
		}
	}
}
