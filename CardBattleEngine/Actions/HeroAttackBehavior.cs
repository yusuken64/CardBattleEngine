namespace CardBattleEngine;

public class HeroAttackBehavior : IAttackBehavior
{
	public int MaxAttacks(IGameEntity attacker)
	{
		return 1;
	}

	public bool CanAttack(IGameEntity attacker, IGameEntity target, GameState state, out string reason)
	{
		if (attacker is not Player hero || !hero.CanAttack()) // weapon, cooldown, etc.
		{
			reason = null;
			return false;
		}

		if (hero.IsFrozen)
		{
			reason = "Hero is Frozen";
			return false;
		}

		if (target == null || !target.IsAlive)
		{
			reason = null;
			return false;
		}

		if (!AttackRules.MustAttackTaunt(attacker, target, state))
		{
			reason = "Must Attack Minion with Taunt";
			return false;
		}

		reason = null;
		return true;
	}

	public IEnumerable<(IGameAction, ActionContext)> GenerateDamageActions(
		IGameEntity attacker,
		IGameEntity target,
		GameState state)
	{
		if (attacker is not Player hero || target == null)
			yield break;

		yield return (
			new DamageAction
			{
				Damage = (Value)hero.Attack
			},
			new ActionContext
			{
				SourcePlayer = hero,
				Source = hero,
				Target = target,
				IsAttack = true,
			});

		hero.HasAttackedThisTurn = true;
		var weapon = hero.EquippedWeapon;
		if (weapon != null)
		{
			weapon.Durability -= 1;

			// 3. If weapon breaks, enqueue DestroyWeaponAction
			if (weapon.Durability <= 0)
			{
				yield return (
					new DestroyWeaponAction(),
					new ActionContext
					{
						SourcePlayer = hero,
						Source = hero,
						Target = hero
					});
			}
		}
		
		// Defender deals retaliatory damage if possible
		if (target is Minion defendingMinion && defendingMinion.IsAlive)
		{
			yield return (new DamageAction()
			{
				Damage = (Value)defendingMinion.Attack
			}, new ActionContext()
			{
				SourcePlayer = defendingMinion.Owner,
				Target = hero,
				Source = defendingMinion,
			});
		}
	}
}
