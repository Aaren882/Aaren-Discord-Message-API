using Arma3WebService.Entity;
using Components.Entity;

namespace Arma3WebService.Managers;

public interface IArma3ActionManager
{
	Task GetAction(Arma3Action action);
}


public sealed class Arma3ActionManager(ServiceAction serviceAction) : IArma3ActionManager
{
	public Task GetAction(Arma3Action action)
	{
		var (connection, payload) = action;
		
		return (payload.Type) switch
		{
			Arma3PayLoadType.Text => serviceAction.TextAction(connection, payload as Arma3PayloadText),
			Arma3PayLoadType.ArrayString => serviceAction.ArrayStringAction(connection, payload as Arma3PayloadArrayString),
			Arma3PayLoadType.JsonString => serviceAction.JsonStringAction(connection, payload as Arma3PayloadJson),
			Arma3PayLoadType.Rpt => serviceAction.RptAction(connection, payload as Arma3PayloadRPT),
			_ => throw new ArgumentOutOfRangeException(nameof(payload.Type), payload.Type, null)
		};
	}
}
