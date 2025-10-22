namespace CardBattleEngine;

internal class AttackAction : IGameAction
{
	private IGameEntity _attacker;
	private IGameEntity _defender;

	public AttackAction(Minion attacker, IGameEntity defender)
	{
		this._attacker = attacker;
		this._defender = defender;
	}

	public bool IsValid(GameState state)
	{
		return _attacker.AttackBehavior.CanAttack(_attacker, _defender, state);
	}

	public IEnumerable<IGameAction> Resolve(GameState state, Player currentPlayer, Player opponent)
	{
		if (_attacker is Minion minion)
		{
			minion.HasAttackedThisTurn = true;
			minion.HasSummoningSickness = false;
		}

		return _attacker.AttackBehavior.GenerateDamageActions(_attacker, _defender, state);
	}

	public override string ToString()
	{
		if (_attacker is Minion minion)
		{
			if (_defender is Minion minion2)
			{
				return $"Attack ({minion.Attack}/{minion.Health}) => ({minion2.Attack}/{minion2.Health})";
			}
			else if (_defender is Player player)
			{
				return $"Attack ({minion.Attack}/{minion.Health}) => {player.Name}";
			}
		}
		return base.ToString();
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
