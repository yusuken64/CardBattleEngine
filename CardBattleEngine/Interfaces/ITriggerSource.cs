namespace CardBattleEngine;

public interface ITriggerSource
{
	Player Owner { get; }
	IEnumerable<TriggeredEffect> TriggeredEffects { get; }
}