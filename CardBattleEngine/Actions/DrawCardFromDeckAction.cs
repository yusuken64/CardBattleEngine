namespace CardBattleEngine;

public class DrawCardFromDeckAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.DrawCard;

	public override bool IsValid(GameState state, ActionContext actionContext) => true;

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		if (actionContext.SourcePlayer.Deck.Any())
		{
			var card = actionContext.SourcePlayer.Deck[0];
			actionContext.SourcePlayer.Deck.RemoveAt(0);
			yield return (new GainCardAction() { Card = card }, actionContext);
		}
		else
		{
			yield return (new FatigueAction(), actionContext);
		}
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