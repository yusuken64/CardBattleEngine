namespace CardBattleEngine.Actions;

internal class GainManaAction : IGameAction
{
	private readonly Player _player;
	private readonly int _amount;

	public GainManaAction(Player player, int amount)
	{
		_player = player;
		_amount = amount;
	}

	public bool IsValid(GameState gameState)
	{
		return true; //potentially need empty mana
	}

	public IEnumerable<IGameAction> Resolve(GameState state, Player currentPlayer, Player opponent)
	{
		_player.Mana = Math.Min(_player.Mana + _amount, 10);
		return [];
	}
}