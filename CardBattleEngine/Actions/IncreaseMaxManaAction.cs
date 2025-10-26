using System.Diagnostics.CodeAnalysis;

namespace CardBattleEngine;

public class IncreaseMaxManaAction : GameActionBase
{
	private readonly Player _player;
	private readonly int _amount;

	public IncreaseMaxManaAction(Player player, int amount)
	{
		_player = player;
		_amount = amount;
	}

	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState state) => _player.MaxMana < 10;

	public override IEnumerable<GameActionBase> Resolve(GameState state, Player current, Player opponent)
	{
		_player.MaxMana = Math.Min(_player.MaxMana + _amount, 10);
		return [];
	}
}
