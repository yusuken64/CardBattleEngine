namespace CardBattleEngine;

public class RefillManaAction : IGameAction
{
	private readonly Player _player;

	public RefillManaAction(Player player) => _player = player;

	public bool IsValid(GameState state) => true;

	public IEnumerable<IGameAction> Resolve(GameState state, Player current, Player opponent)
	{
		_player.Mana = _player.MaxMana;
		return [];
	}
}