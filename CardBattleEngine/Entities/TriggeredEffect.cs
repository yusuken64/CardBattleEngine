using Newtonsoft.Json.Converters;

namespace CardBattleEngine;

public class TriggeredEffect : ITriggeredEffect
{
	public EffectTrigger EffectTrigger { get; set; }
	public EffectTiming EffectTiming { get; set; }
	public TargetingType TargetType { get; set; } //TODO remove
	public List<IGameAction> GameActions { get; set; } = new();
	public ITriggerCondition Condition { get; set; }
	public IAffectedEntitySelector AffectedEntitySelector { get; set; }
	public ExpirationTrigger ExpirationTrigger { get; set; }
	internal TriggeredEffect CloneFor(Minion minion)
	{
		return new TriggeredEffect()
		{
			EffectTiming = this.EffectTiming,
			EffectTrigger = this.EffectTrigger,
			TargetType = TargetType,
			AffectedEntitySelector = AffectedEntitySelector,
			Condition = Condition,
			GameActions = GameActions.ToList(), //TODO implement deep clone for effects
		};
	}
}

public class ExpirationTrigger : ITriggeredEffect
{
	public EffectTrigger EffectTrigger { get; set; }
	public EffectTiming EffectTiming { get; set; }
	public ITriggerCondition Condition { get; set; }
	public int CountDown { get; set; }
}

public interface ITriggeredEffect
{
	public EffectTrigger EffectTrigger { get; set; }
	public EffectTiming EffectTiming { get; set; }
	public ITriggerCondition Condition { get; set; }
}


[JsonConverter(typeof(StringEnumConverter))]
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

[JsonConverter(typeof(StringEnumConverter))]
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
	SpellCountered,
	SecretCasted,
	SecretResolved,
	EquipWeapon,
	OnHeroPower,
	OnHealed,
	Reborn,
	GameEnd,
} //TODO standardize naming

[JsonConverter(typeof(StringEnumConverter))]
public enum EffectTiming
{
	Pre,
	Post,
	Persistant //Aura don't trigger
}