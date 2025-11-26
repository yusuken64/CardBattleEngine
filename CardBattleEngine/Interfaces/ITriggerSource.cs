namespace CardBattleEngine;

public interface ITriggerSource
{
	IGameEntity Entity { get; }
	IEnumerable<TriggeredEffect> TriggeredEffects { get; }
}