using System.Net.WebSockets;

namespace Arma3WebService.Entity;

public record WebsocketContextEntity(HttpContext Context, Arma3PayLoadType ConnectionType);
public record WebsocketEntity(WebSocket WebSocket, WebsocketContextEntity ContextEntity);
