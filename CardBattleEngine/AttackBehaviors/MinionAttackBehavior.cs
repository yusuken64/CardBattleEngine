namespace CardBattleEngine;

public class MinionAttackBehavior : IAttackBehavior
{
	public bool CanInitiateAttack(IGameEntity attacker, out string reason)
	{
		if (attacker is not Minion minion)
		{
			reason = null;
			return false;
		}

		if (!minion.IsAlive)
		{
			reason = null;
			return false;
		}

		if (minion.CannotAttack)
		{
			reason = null;
			return false;
		}

		if (minion.IsFrozen)
		{
			reason = "Frozen Characters can't Attack";
			return false;
		}

		if (minion.Attack <= 0)
		{
			reason = "";
			return false;
		}

		// Windfury / attack count
		if (minion.AttacksPerformedThisTurn >= MaxAttacks(attacker))
		{
			reason = "Already Attacked";
			return false;
		}

		// Summoning sickness handled later because Rush depends on target type
		if (minion.HasSummoningSickness && !minion.HasCharge && !minion.HasRush)
		{
			reason = "Not Ready to Attack";
			return false;
		}

		reason = null;
		return true;
	}

	public bool IsValidAttackTarget(IGameEntity attacker, IGameEntity target, GameState state, out string reason)
	{
		var minion = (Minion)attacker;

		// Target must be alive
		if (!target.IsAlive)
		{
			reason = null;
			return false;
		}

		// Cannot attack friendly units
		if (minion.Owner == target.Owner)
		{
			reason = null;
			return false;
		}

		// Cannot attack stealth minions
		if (target is Minion tminion && tminion.IsStealth)
		{
			reason = "Target is Stealthed";
			return false;
		}

		// Rush restriction (only on first turn)
		if (minion.HasSummoningSickness && minion.HasRush && target is not Minion)
		{
			reason = "Must Attack another minion";
			return false;
		}

		// Taunt rules (board scan)
		if (!AttackRules.MustAttackTaunt(minion, target, state))
		{
			reason = "Must Attack Minion with Taunt";
			return false;
		}

		reason = null;
		return true;
	}

	public bool CanAttack(IGameEntity attacker, IGameEntity target, GameState state, out string reason)
	{
		if (!CanInitiateAttack(attacker, out reason))
			return false;

		return IsValidAttackTarget(attacker, target, state, out reason);
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
			IsAttack = true,
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