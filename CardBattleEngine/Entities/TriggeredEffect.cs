namespace CardBattleEngine;

public class TriggeredEffect
{
	public EffectTrigger EffectTrigger { get; set; }
	public EffectTiming EffectTiming { get; set; }
	public TargetType TargetType { get; set; }
	public List<IGameAction> GameActions { get; set; } = new();
	public ITriggerCondition Condition { get; set; }

	internal TriggeredEffect CloneFor(Minion minion)
	{
		return new TriggeredEffect()
		{
			EffectTiming = this.EffectTiming,
			EffectTrigger = this.EffectTrigger,
			TargetType = TargetType,
			GameActions = GameActions.ToList(), //TODO implement deep clone for effects
		};
	}
}
public enum TargetType
{
	Any,
	FriendlyMinion,
	FriendlyHero,
	EnemyMinion,
	EnemyHero,
	AnyEnemy,
	Self
}

public enum EffectTrigger
{
	None,
	Battlecry,
	Deathrattle,
	OnPlay,
	SummonMinion,
	Attack,
	OnDamage,
	Aura,
	OnDeath,
	DrawCard,
	GameStart,
	TurnStart,
	OnTurnEnd,
	OnFreeze,
} //TODO standardize naming

public enum EffectTiming
{
	Pre,
	Post
}