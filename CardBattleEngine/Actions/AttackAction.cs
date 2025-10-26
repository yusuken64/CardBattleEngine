namespace CardBattleEngine;

internal class AttackAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.Attack;

	public override bool IsValid(GameState state, ActionContext context)
	{
		return context.Source.AttackBehavior.CanAttack(context.Source, context.Target, state);
	}

	public override IEnumerable<GameActionBase> Resolve(GameState state, ActionContext context)
	{
		if (context.Source is Minion minion)
		{
			minion.HasAttackedThisTurn = true;
			minion.HasSummoningSickness = false;
		}

		return context.Source.AttackBehavior.GenerateDamageActions(context.Source, context.Target, state);
	}
}

public static class AttackRules
{
	public static bool MustAttackTaunt(IGameEntity attacker, IGameEntity target, GameState state)
	{
		// Find opponent's taunt minions
		var opponent = state.OpponentOf(attacker.Owner);
		var taunts = opponent.Board.Where(m => m.Taunt && m.IsAlive);

		// If there are taunts, you must target one
		return !taunts.Any() || (target is Minion t && t.Taunt);
	}
}
