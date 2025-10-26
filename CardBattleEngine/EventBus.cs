namespace CardBattleEngine;

public class EventBus
{
	private readonly List<Action<IGameEvent>> _subs = new();
	private readonly List<ITrigger> _triggers = new();
	public void Subscribe(Action<IGameEvent> handler) => _subs.Add(handler);
	public void Unsubscribe(Action<IGameEvent> handler) => _subs.Remove(handler);
	public void Raise(IGameEvent ev)
	{
		// copy to avoid modification during enumeration
		var copy = _subs.ToArray();
		foreach (var s in copy) s(ev);
	}

	public void RegisterTrigger(ITrigger trigger) => _triggers.Add(trigger);
	public void UnregisterTrigger(ITrigger trigger) => _triggers.Remove(trigger);

	internal IEnumerable<(IGameAction action, ActionContext context)> GetPreTriggers(GameState gameState, IGameAction action)
	{
		return gameState.GetAllEntities()
			.SelectMany(x => x.TriggeredEffects)
			.Where(te => te.EffectTrigger == action.EffectTrigger &&
						 te.EffectTiming == EffectTiming.Pre) //Where matches action
			.SelectMany(te => te.GameActions.Select(ga =>
			(
				ga,
				new ActionContext
				{
					//Source = te.Owner,
					//SourcePlayer = te.Owner.Player,
					//TriggeringAction = triggeringAction
				}
			)
		));
	}

	internal IEnumerable<(IGameAction action, ActionContext context)> GetPostTriggers(GameState gameState, IGameAction action)
	{
		return gameState.GetAllEntities()
			.SelectMany(x => x.TriggeredEffects)
			.Where(te => te.EffectTrigger == action.EffectTrigger &&
						te.EffectTiming == EffectTiming.Post)
			.SelectMany(te => te.GameActions.Select(ga =>
			(
				ga,
				new ActionContext
				{
					//Source = te.Owner,
					//SourcePlayer = te.Owner.Player,
					//TriggeringAction = triggeringAction
				}
			)
		));
	}
}
