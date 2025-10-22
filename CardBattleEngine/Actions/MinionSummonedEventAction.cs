namespace CardBattleEngine;

internal class MinionSummonedEventAction : IGameAction
{
	public Minion Minion { get; }

	public MinionSummonedEventAction(Minion minion)
	{
		Minion = minion;
	}

	public bool IsValid(GameState state) => Minion.IsAlive;

	public IEnumerable<IGameAction> Resolve(GameState state, Player currentPlayer, Player opponent)
	{
		// This action itself does not resolve any effects; 
		// it exists so triggers can respond to it
		return [];
	}
}