namespace CardBattleEngine;

public class DrawCardAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.DrawCard;

	public override bool IsValid(GameState state, ActionContext actionContext) => actionContext.SourcePlayer.Deck.Count > 0;

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		var card = actionContext.SourcePlayer.Deck[0];
		actionContext.SourcePlayer.Deck.RemoveAt(0);
		actionContext.SourcePlayer.Hand.Add(card);

		// Could return further side effects (triggers)
		return [];
	}
}