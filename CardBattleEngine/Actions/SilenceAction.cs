namespace CardBattleEngine;

public class SilenceAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		// Valid if the target exists and is alive
		return context.Target != null && context.Target.IsAlive;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (!IsValid(state, context, out string _))
			yield break;

		// Apply freeze
		if (context.Target is Minion minion)
		{
			minion.IsFrozen = false;
			minion.IsStealth = false;
			minion.HasCharge = false;
			minion.HasDivineShield = false;
			minion.HasPoisonous = false;
			minion.Taunt = false;
			minion.HasRush = false;
			minion.HasWindfury = false;
			minion.HasLifeSteal = false;
			minion.HasReborn = false;
			minion.CannotAttack = false;
			minion.ClearModifiers();
			minion.TriggeredEffects.Clear();
		}

		yield break; // no side-effects
	}
}
