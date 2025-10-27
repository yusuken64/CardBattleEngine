namespace CardBattleEngine;

public class DrawCardFromDeckAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.DrawCard;

	public override bool IsValid(GameState state, ActionContext actionContext) => actionContext.SourcePlayer.Deck.Count > 0;

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		var card = actionContext.SourcePlayer.Deck[0];
		actionContext.SourcePlayer.Deck.RemoveAt(0);
		
		return [(new GainCardAction() { Card = card }, actionContext)];
	}
}

public class GainCardAction : GameActionBase
{
	public Card Card { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context)
	{
		return context.SourcePlayer.Hand.Count() < 10;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		if (!IsValid(state, actionContext)) { return []; }

		actionContext.SourcePlayer.Hand.Add(Card);

		return [];
	}
}