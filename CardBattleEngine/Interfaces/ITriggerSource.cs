namespace CardBattleEngine;

public interface ITriggerSource
{
	public IGameEntity Entity { get; }
	public List<TriggeredEffect> TriggeredEffects { get; }
}