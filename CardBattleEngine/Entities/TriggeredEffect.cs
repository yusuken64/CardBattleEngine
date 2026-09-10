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
	public TargetRequirement TargetRequirement { get; set; }
	public ExpirationTrigger ExpirationTrigger { get; set; }
	public EffectFrequency Frequency { get; set; } = EffectFrequency.Unlimited;
	public bool UsedThisTurn { get; set; } = false;
	internal TriggeredEffect Clone()
	{
		return new TriggeredEffect()
		{
			EffectTiming = this.EffectTiming,
			EffectTrigger = this.EffectTrigger,
			Scope = this.Scope,
			AffectedEntitySelector = AffectedEntitySelector,
			TargetRequirement = TargetRequirement,
			Condition = Condition,
			// IGameAction no longer carries per-resolution state (Canceled moved to ActionContext),
			// so instances are safe to share across clones instead of deep-cloning.
			GameActions = GameActions,
			Frequency = this.Frequency,
			UsedThisTurn = this.UsedThisTurn,
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

[JsonConverter(typeof(StringEnumConverter))]
public enum EffectFrequency
{
	Unlimited,
	OncePerTurn,
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
	OnRelicAbility,
} //TODO standardize naming

[JsonConverter(typeof(StringEnumConverter))]
public enum EffectTiming
{
	Pre,
	Post,
	Persistant //Aura don't trigger
}