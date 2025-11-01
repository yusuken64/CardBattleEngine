namespace CardBattleEngine.Actions;

public class HeroAttackBehavior : IAttackBehavior
{
	public bool CanAttack(IGameEntity attacker, IGameEntity target, GameState state)
	{
		if (attacker is not Player hero || !hero.CanAttack()) // weapon, cooldown, etc.
			return false;

		if (hero.IsFrozen)
		{
			return false;
		}

		if (target == null || !target.IsAlive)
			return false;

		if (!AttackRules.MustAttackTaunt(attacker, target, state))
			return false;

		return true; // Could also enforce taunt, special hero rules, etc.
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
				Damage = hero.Attack
			},
			new ActionContext
			{
				SourcePlayer = hero,
				Source = hero,
				Target = target
			});

		hero.HasAttacked = true;
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
	}

}
