using Arma3WebService.Entity;

namespace Arma3WebService.Factory;

public sealed class WebsocketContextEntityFactory
{
	/* public WebsocketContextEntity CreateTextContext(HttpContext httpContext)
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
	public WebsocketContextEntity CreateFlatJsonStringContext(HttpContext httpContext)
	{
		return new WebsocketContextEntity(httpContext, actionFactory);
	} */
	public WebsocketContextEntity CreateJsonStringContext(HttpContext httpContext)
	{
		return new WebsocketContextEntity(httpContext);
	}
}
