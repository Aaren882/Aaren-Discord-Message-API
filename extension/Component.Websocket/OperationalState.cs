namespace Component.Websocket;

public enum OperationalState
{
	Empty, //- _websocket is not created
	Abort, //- _websocket is exist but with no connection (can be aborted)
	Idle, Working //- Working States
}
