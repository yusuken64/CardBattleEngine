using System.Text.Json.Serialization;

namespace CardBattleEngine;

public class TriggeredEffect
{
	public EffectTrigger EffectTrigger { get; set; }
	public EffectTiming EffectTiming { get; set; }
	public TargetingType TargetType { get; set; }
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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TargetingType
{
	Any,
	FriendlyMinion,
	FriendlyHero,
	EnemyMinion,
	EnemyHero,
	AnyEnemy,
	Self,
	None, //Spell with not target
	AnyMinion,
}

[JsonConverter(typeof(JsonStringEnumConverter))]
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
	SpellCast,
} //TODO standardize naming

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EffectTiming
{
	Pre,
	Post
}