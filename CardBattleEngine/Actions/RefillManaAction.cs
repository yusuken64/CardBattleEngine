namespace CardBattleEngine;

public class RefillManaAction : GameActionBase
{
	private readonly Player _player;

	public RefillManaAction(Player player) => _player = player;

	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState state) => true;

	public override IEnumerable<GameActionBase> Resolve(GameState state, Player current, Player opponent)
	{
		_player.Mana = _player.MaxMana;
		return [];
	}
}