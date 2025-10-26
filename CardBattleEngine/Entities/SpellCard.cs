
namespace CardBattleEngine;

public class SpellCard : Card
{
	public override Card Clone()
	{
		throw new NotImplementedException();
	}

	internal override IEnumerable<(IGameAction, ActionContext)> GetPlayEffects(GameState state, Player currentPlayer, Player opponent)
	{
		throw new NotImplementedException();
	}
}