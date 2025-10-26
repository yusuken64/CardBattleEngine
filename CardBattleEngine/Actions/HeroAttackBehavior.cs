namespace CardBattleEngine.Actions;

public class HeroAttackBehavior : IAttackBehavior
{
	public bool CanAttack(IGameEntity attacker, IGameEntity target, GameState state)
	{
		if (attacker is not Player hero || !hero.CanAttack) // weapon, cooldown, etc.
			return false;

		if (target == null || !target.IsAlive)
			return false;

		if (!AttackRules.MustAttackTaunt(attacker, target, state))
			return false;

		return true; // Could also enforce taunt, special hero rules, etc.
	}

	public IEnumerable<GameActionBase> GenerateDamageActions(IGameEntity attacker, IGameEntity target, GameState state)
	{
		if (attacker is not Player hero || target == null)
			return [];

		hero.HasAttacked = true; // or weapon usage

		return
		[
			new DamageAction()
			{
				Damage = target.Attack
			}
        ];
	}
}
