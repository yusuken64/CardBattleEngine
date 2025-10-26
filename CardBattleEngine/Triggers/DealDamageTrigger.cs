namespace CardBattleEngine;

public class DealDamageTrigger : IPostTrigger
{
	private readonly IGameEntity _source;
	private readonly Func<GameState, int> _damageCalculator;
	private readonly Func<GameState, IGameEntity> _targetSelector;

	public DealDamageTrigger(IGameEntity source, Func<GameState, int> damageCalculator, Func<GameState, IGameEntity> targetSelector)
	{
		_source = source;
		_damageCalculator = damageCalculator;
		_targetSelector = targetSelector;
	}

	public bool CheckCondition(GameState state, GameActionBase action)
	{
		//return action is PlayCardAction play && play.Card.Owner == _source;
		return true;
	}

	public GameActionBase GenerateAction(GameState gameState)
	{
		var target = _targetSelector(gameState);
		if (target == null) return null;

		int damage = _damageCalculator(gameState);
		return new DamageAction()
		{
			Damage = damage
		};
	}
}
