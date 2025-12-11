namespace CardBattleEngine;

public interface IAttackBehavior
{
	public int MaxAttacks(IGameEntity attacker);
	bool CanAttack(IGameEntity attacker, IGameEntity target, GameState state);
	IEnumerable<(IGameAction, ActionContext)> GenerateDamageActions(IGameEntity attacker, IGameEntity target, GameState state);
}
public class MinionAttackBehavior : IAttackBehavior
{
	public bool CanAttack(IGameEntity attacker, IGameEntity target, GameState state)
	{
		if (attacker is not Minion minion)
			return false;

		// Target must be alive
		if (!target.IsAlive)
			return false;

		if (minion.CannotAttack)
			return false;

		// Cannot attack friendly units
		if (minion.Owner == target.Owner)
			return false;

		// Attacker is frozen
		if (minion.IsFrozen)
			return false;

		// Cannot attack stealth minions
		if (target is Minion tminion && tminion.IsStealth)
			return false;

		// Must obey taunt rules
		if (!AttackRules.MustAttackTaunt(minion, target, state))
			return false;

		// ----- WIND FURY / ATTACK COUNT -----
		if (minion.AttacksPerformedThisTurn >= MaxAttacks(attacker))
			return false;

		// ----- SUMMONING SICKNESS / RUSH / CHARGE -----
		if (minion.HasSummoningSickness)
		{
			if (minion.HasCharge)
			{
				// Charge = can attack anything
			}
			else if (minion.HasRush)
			{
				// Rush = can ONLY attack minions on first turn
				if (target is not Minion)
					return false;
			}
			else
			{
				// No charge, no rush
				return false;
			}
		}

		return true;
	}

	public IEnumerable<(IGameAction, ActionContext)> GenerateDamageActions(IGameEntity attacker, IGameEntity target, GameState state)
	{
		if (attacker is not Minion attackingMinion)
			yield break;

		// Attacker deals damage to target
		yield return (new DamageAction()
		{
			Damage = (Value)attackingMinion.Attack
		},
		new ActionContext()
		{
			SourcePlayer = attackingMinion.Owner,
			Source = attackingMinion,
			Target = target,
		});

		// Defender deals retaliatory damage if possible
		if (target is Minion defendingMinion && defendingMinion.IsAlive)
		{
			yield return (new DamageAction()
			{
				Damage = (Value)defendingMinion.Attack
			}, new ActionContext()
			{
				SourcePlayer = attackingMinion.Owner,
				Target = attackingMinion,
				Source = defendingMinion,
			});
		}
	}

	public int MaxAttacks(IGameEntity attacker)
	{
		if (attacker is Minion minion)
		{
			//if (minion.HasMegaWindfury) return 4;
			if (minion.HasWindfury) return 2;
		}
		return 1;
	}
}