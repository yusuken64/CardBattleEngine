namespace CardBattleEngine;

internal class DamageAction : IGameAction
{
	public IGameEntity Target { get; }
	public int Damage { get; }

	public DamageAction(IGameEntity target, int damage)
	{
		Target = target;
		Damage = damage;
	}

	public bool IsValid(GameState state)
	{
		// Valid if target is still alive / on board
		return Target != null && Target.IsAlive;
	}

	public IEnumerable<IGameAction> Resolve(GameState state, Player currentPlayer, Player opponent)
	{
		if (!IsValid(state))
			return [];

		// Apply damage
		Target.Health -= Damage;

		var sideEffects = new List<IGameAction>();

		// Check for death triggers
		if (Target.Health <= 0)
		{
			sideEffects.Add(new DeathAction(Target));
		}

		return sideEffects;
	}
}