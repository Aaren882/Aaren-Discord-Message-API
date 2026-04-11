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
		
		return payload.Type switch
		{
			Arma3PayLoadType.Text => 
				serviceAction.TextAction(connection, (Arma3PayloadText) payload),
			Arma3PayLoadType.Rpt =>
				serviceAction.RptAction(connection, (Arma3PayloadRPT) payload),
			Arma3PayLoadType.Command =>
				serviceAction.CallBackAction(connection, (Arma3PayloadCallBack) payload),
			Arma3PayLoadType.JsonString =>
				serviceAction.JsonStringAction(connection, (Arma3PayloadJson) payload),
			Arma3PayLoadType.ArrayString => 
				serviceAction.ArrayStringAction(connection, (Arma3PayloadArrayString) payload),
			
			_ => throw new ArgumentOutOfRangeException(nameof(payload.Type), payload.Type, null)
		};
	}
}
