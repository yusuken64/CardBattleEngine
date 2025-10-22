namespace CardBattleEngine;

public interface IAttackBehavior
{
	bool CanAttack(IGameEntity attacker, IGameEntity target, GameState state);
	IEnumerable<IGameAction> GenerateDamageActions(IGameEntity attacker, IGameEntity target, GameState state);
}
internal class MinionAttackBehavior : IAttackBehavior
{
	public bool CanAttack(IGameEntity attacker, IGameEntity target, GameState state)
	{
		if (attacker is not Minion attackingMinion)
			return false;

		// Can't attack friendly units
		if (attackingMinion.Owner == target.Owner)
			return false;

		// Can't attack dead targets
		if (!target.IsAlive)
			return false;

		// Respect taunt via centralized rule
		if (!AttackRules.MustAttackTaunt(attacker, target, state))
			return false;

		// Check for exhaustion or summoning sickness
		if (attackingMinion.HasSummoningSickness || attackingMinion.HasAttackedThisTurn)
			return false;

		return true;
	}

	public IEnumerable<IGameAction> GenerateDamageActions(IGameEntity attacker, IGameEntity target, GameState state)
	{
		if (attacker is not Minion attackingMinion)
			yield break;

		// Attacker deals damage to target
		yield return new DamageAction(target, attackingMinion.Attack);

		// Defender deals retaliatory damage if possible
		if (target is Minion defendingMinion && defendingMinion.IsAlive)
			yield return new DamageAction(attackingMinion, defendingMinion.Attack);
		//else if (target is Player defendingHero)
		//	yield return new DamageAction(defendingHero, attackingMinion.Attack);
	}
}