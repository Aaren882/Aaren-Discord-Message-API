namespace ServiceConnection.Entity;

public record struct CallContext(
	UInt64 steamId,
	string fileSource,
	string missionName,
	string serverName,
	Int16 remoteExecutedOwner
);

public delegate int ExtensionCallback(
	string name, 
	string function,
	string data
);
