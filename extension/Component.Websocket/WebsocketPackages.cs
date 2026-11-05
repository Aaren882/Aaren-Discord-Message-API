using System.Net.WebSockets;

namespace Component.Websocket;

public readonly record struct OutboundMessage(ReadOnlyMemory<byte> MessageBytes, WebSocketMessageType MessageType, bool EndOfMessage);
public readonly record struct InboundMessage(ReadOnlyMemory<byte> Bytes, WebSocketMessageType MessageType, bool EndOfMessage);
