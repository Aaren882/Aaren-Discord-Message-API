using System.Collections.Concurrent;

namespace Arma3WebService.Broker;

public class BinaryPayloadBroker
{
	private readonly ConcurrentDictionary<string, Action> _subscriber = new();
	public bool TryAdd(string actionName, Action action)
	{
		return _subscriber.TryAdd(actionName, action);
	}
	public bool TryRemove(string actionName)
	{
		return _subscriber.TryRemove(actionName, out _);
	}
	public void Publish(string actionName)
	{
		if (!_subscriber.TryGetValue(actionName, out var action))
			throw new ArgumentOutOfRangeException(nameof(actionName));
		action.Invoke();
	}
}
