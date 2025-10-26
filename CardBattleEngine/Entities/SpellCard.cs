
namespace CardBattleEngine;

public class SpellCard : Card
{
	public override Card Clone()
	{
		throw new NotImplementedException();
	}

	internal override IEnumerable<(IGameAction, ActionContext)> GetPlayEffects(GameState state, ActionContext actionContext)
	{
		throw new NotImplementedException();
	}
}