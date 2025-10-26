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

	internal IEnumerable<IGameAction> GetPreTriggers(GameState gameState, IGameAction action)
	{
		return gameState.GetAllEntities()
			.SelectMany(x => x.TriggeredEffects)
			.Where(x => x.EffectTrigger == action.EffectTrigger) //Where matches action
			.SelectMany(x => x.GameActions);
	}

	internal IEnumerable<IGameAction> GetPostTriggers(GameState gameState, IGameAction action)
	{
		return gameState.GetAllEntities()
			.SelectMany(x => x.TriggeredEffects)
			.Where(x => x.EffectTrigger == action.EffectTrigger) //Where matches action
			.SelectMany(x => x.GameActions);
	}
}
