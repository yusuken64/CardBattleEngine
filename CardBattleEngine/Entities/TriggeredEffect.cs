using Newtonsoft.Json.Converters;

namespace CardBattleEngine;

public class TriggeredEffect : ITriggeredEffect
{
	public EffectTrigger EffectTrigger { get; set; }
	public EffectTiming EffectTiming { get; set; }
	public TriggerScope Scope { get; set; }
	public List<IGameAction> GameActions { get; set; } = new();
	public ITriggerCondition Condition { get; set; }
	public IAffectedEntitySelector AffectedEntitySelector { get; set; }
	public ExpirationTrigger ExpirationTrigger { get; set; }
	internal TriggeredEffect Clone()
	{
		return new TriggeredEffect()
		{
			EffectTiming = this.EffectTiming,
			EffectTrigger = this.EffectTrigger,
			Scope = this.Scope,
			AffectedEntitySelector = AffectedEntitySelector,
			Condition = Condition,
			GameActions = GameActions.Select(a => a.Clone()).ToList(),
		};
	}
}

public class ExpirationTrigger : ITriggeredEffect
{
	public EffectTrigger EffectTrigger { get; set; }
	public EffectTiming EffectTiming { get; set; }
	public TriggerScope Scope { get; set; }
	public ITriggerCondition Condition { get; set; }
	public int CountDown { get; set; }
}

public interface ITriggeredEffect
{
	public EffectTrigger EffectTrigger { get; set; }
	public EffectTiming EffectTiming { get; set; }
	public TriggerScope Scope { get; set; }
	public ITriggerCondition Condition { get; set; }
}

// Self: the trigger only fires when the entity holding it is the one that performed the triggering action
// (e.g. "when this minion attacks"). Any: fires regardless of which entity performed the action (default).
[JsonConverter(typeof(StringEnumConverter))]
public enum TriggerScope
{
	Any,
	Self,
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