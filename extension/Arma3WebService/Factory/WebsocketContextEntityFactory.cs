using Arma3WebService.Entity;
using Arma3WebService.Managers;
using Arma3WebService.Models;

namespace Arma3WebService.Factory;

public sealed class WebsocketContextEntityFactory(IArma3ActionFactory actionFactory)
{
	public WebsocketContextEntity CreateTextContext(HttpContext httpContext)
	{
		return new WebsocketContextEntity(httpContext, actionFactory);
	}
	public WebsocketContextEntity CreateRptContext(HttpContext httpContext)
	{
		return new WebsocketContextEntity(httpContext, actionFactory);
	}
	public WebsocketContextEntity CreateCommandContext(HttpContext httpContext)
	{
		return new WebsocketContextEntity(httpContext, actionFactory);
	}
	public WebsocketContextEntity CreateGameInfoContext(HttpContext httpContext)
	{
		return new WebsocketContextEntity(httpContext, actionFactory);
	}
	public WebsocketContextEntity CreateJsonStringContext(HttpContext httpContext)
	{
		return new WebsocketContextEntity(httpContext, actionFactory);
	}
	public WebsocketContextEntity CreateFlatJsonStringContext(HttpContext httpContext)
	{
		return new WebsocketContextEntity(httpContext, actionFactory);
	}
}
