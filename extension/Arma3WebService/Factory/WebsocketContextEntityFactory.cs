using Arma3WebService.Entity;
using Arma3WebService.Managers;
using Arma3WebService.Models;

namespace Arma3WebService.Factory;

public class WebsocketContextEntityFactory
{
	public WebsocketContextEntity CreateTextContext(HttpContext httpContext, IWebSocketService webSocketService, IArma3ActionManager actionManager)
	{
		return new WebsocketContextEntity(httpContext, webSocketService, actionManager);
	}
	public WebsocketContextEntity CreateRptContext(HttpContext httpContext, IWebSocketService webSocketService, IArma3ActionManager actionManager)
	{
		return new WebsocketContextEntity(httpContext, webSocketService, actionManager);
	}
	public WebsocketContextEntity CreateCommandContext(HttpContext httpContext, IWebSocketService webSocketService, IArma3ActionManager actionManager)
	{
		return new WebsocketContextEntity(httpContext, webSocketService, actionManager);
	}
	public WebsocketContextEntity CreateGameInfoContext(HttpContext httpContext, IWebSocketService webSocketService, IArma3ActionManager actionManager)
	{
		return new WebsocketContextEntity(httpContext, webSocketService, actionManager);
	}
	public WebsocketContextEntity CreateJsonStringContext(HttpContext httpContext, IWebSocketService webSocketService, IArma3ActionManager actionManager)
	{
		return new WebsocketContextEntity(httpContext, webSocketService, actionManager);
	}
	public WebsocketContextEntity CreateArrayStringContext(HttpContext httpContext, IWebSocketService webSocketService, IArma3ActionManager actionManager)
	{
		return new WebsocketContextEntity(httpContext, webSocketService, actionManager);
	}
}
