namespace CardBattleEngine;

public class SpellCard : Card
{
	internal override IEnumerable<IGameAction> GetPlayEffects(GameState state, Player currentPlayer, Player opponent)
	{
		throw new NotImplementedException();
	}
}