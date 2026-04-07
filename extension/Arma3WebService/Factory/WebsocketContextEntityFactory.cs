using Arma3WebService.Entity;
using Arma3WebService.Managers;
using Arma3WebService.Models;

namespace Arma3WebService.Factory;

public sealed class WebsocketContextEntityFactory(IArma3ActionFactory actionFactory)
{
	public WebsocketContextEntity CreateTextContext(HttpContext httpContext, IWebSocketService webSocketService)
	{
		return new WebsocketContextEntity(httpContext, webSocketService, actionFactory);
	}
	public WebsocketContextEntity CreateRptContext(HttpContext httpContext, IWebSocketService webSocketService)
	{
		return new WebsocketContextEntity(httpContext, webSocketService, actionFactory);
	}
	public WebsocketContextEntity CreateCommandContext(HttpContext httpContext, IWebSocketService webSocketService)
	{
		return new WebsocketContextEntity(httpContext, webSocketService, actionFactory);
	}
	public WebsocketContextEntity CreateGameInfoContext(HttpContext httpContext, IWebSocketService webSocketService)
	{
		return new WebsocketContextEntity(httpContext, webSocketService, actionFactory);
	}
	public WebsocketContextEntity CreateJsonStringContext(HttpContext httpContext, IWebSocketService webSocketService)
	{
		return new WebsocketContextEntity(httpContext, webSocketService, actionFactory);
	}
	public WebsocketContextEntity CreateArrayStringContext(HttpContext httpContext, IWebSocketService webSocketService)
	{
		return new WebsocketContextEntity(httpContext, webSocketService, actionFactory);
	}
}
