namespace CardBattleEngine;

internal class DeathAction : GameActionBase
{
	private readonly IGameEntity _target;

	public DeathAction(IGameEntity target)
	{
		_target = target;
	}

	public override EffectTrigger EffectTrigger => EffectTrigger.OnDeath;

	public override bool IsValid(GameState state)
	{
		return _target.IsAlive;
	}

	public override IEnumerable<IGameAction> Resolve(GameState state, Player currentPlayer, Player opponent)
	{
		var sideEffects = new List<IGameAction>();
		_target.IsAlive = false;
		if (_target is Minion minion)
		{
			minion.Owner.Board.Remove(minion);
			minion.Owner.Graveyard.Add(minion);

			foreach (var effect in minion.TriggeredEffects
				.Where(x => x.EffectTrigger == EffectTrigger.Deathrattle))
			{
				sideEffects.AddRange(effect.GameActions);
			}
		}

		return sideEffects;
	}
}