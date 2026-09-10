namespace CardBattleEngine;

public class InertAttackBehavior : IAttackBehavior
{
	public static readonly InertAttackBehavior Instance = new();

	public int MaxAttacks(IGameEntity attacker) => 0;

	public bool CanInitiateAttack(IGameEntity attacker, out string reason)
	{
		reason = null;
		return false;
	}

	public bool IsValidAttackTarget(IGameEntity attacker, IGameEntity target, GameState state, out string reason)
	{
		reason = null;
		return false;
	}

	public bool CanAttack(IGameEntity attacker, IGameEntity target, GameState state, out string reason)
	{
		reason = null;
		return false;
	}

	public IEnumerable<(IGameAction, ActionContext)> GenerateDamageActions(IGameEntity attacker, IGameEntity target, GameState state)
	{
		return Enumerable.Empty<(IGameAction, ActionContext)>();
	}
}
