using System.Numerics;

namespace CardBattleEngine;

internal class StartTurnAction : GameActionBase
{
	private Player _currentPlayer;

	public StartTurnAction(Player currentPlayer)
	{
		this._currentPlayer = currentPlayer;
	}

	public override EffectTrigger EffectTrigger => EffectTrigger.TurnStart;
	public override bool IsValid(GameState state) => true;

	public override IEnumerable<GameActionBase> Resolve(GameState state, Player currentPlayer, Player opponent)
	{
		// Reset attack flags
		foreach (var minion in _currentPlayer.Board)
		{
			minion.HasAttackedThisTurn = false;
			minion.HasSummoningSickness = false;
		}

		return
		[
			new IncreaseMaxManaAction(_currentPlayer, 1),
			new RefillManaAction(_currentPlayer),
			new DrawCardAction(_currentPlayer)
		];
	}
}
