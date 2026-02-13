namespace CardBattleEngine;

public interface IAttackBehavior
{
	public int MaxAttacks(IGameEntity attacker);
	bool CanAttack(IGameEntity attacker, IGameEntity target, GameState state, out string reason);
	IEnumerable<(IGameAction, ActionContext)> GenerateDamageActions(IGameEntity attacker, IGameEntity target, GameState state);
	bool CanInitiateAttack(IGameEntity attacker, out string reason);
	bool IsValidAttackTarget(IGameEntity attacker, IGameEntity target, GameState state, out string reason);
}
