namespace CardBattleEngine;

public class DrawCardFromDeckAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.DrawCard;

	public override bool IsValid(GameState state, ActionContext actionContext, out string reason)
	{
		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		if (actionContext.SourcePlayer.Deck.Any())
		{
			var card = actionContext.SourcePlayer.Deck[0];
			actionContext.SourcePlayer.Deck.RemoveAt(0);
			actionContext.CardsLeftInDeck = actionContext.SourcePlayer.Deck.Count;
			yield return (new GainCardAction() { Card = card }, actionContext);
		}
		else
		{
			yield return (new FatigueAction(), actionContext);
		}
	}
}
public class DrawTargetCardFromDeckAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.DrawCard;

	public override bool IsValid(GameState state, ActionContext actionContext, out string reason)
	{
		reason = null;
		var single = actionContext.Targets?.FirstOrDefault();
		return single != null && actionContext.SourcePlayer.Deck.Contains(single);
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		var single = actionContext.Targets?.FirstOrDefault();
		var card = actionContext.SourcePlayer.Deck.FirstOrDefault(x => x == single);
		if (card != null)
		{
			actionContext.SourcePlayer.Deck.Remove(card);
			yield return (new GainCardAction() { Card = card }, actionContext);
		}
	}
}

public class GainCardAction : GameActionBase
{
	public Card Card { get; set; }
	public bool GenerateNewCard { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		return context.SourcePlayer.Hand.Count() < 10;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		if (!IsValid(state, actionContext, out var _)) { return []; }

		Card card;
		if (GenerateNewCard)
		{
			card = Card.Clone();
			card.Id = Guid.NewGuid();
		}
		else
		{
			card = Card;
		}

		card.Owner = actionContext.SourcePlayer;
		actionContext.CardGained = card;
		actionContext.SourcePlayer.Hand.Add(card);

		return [];
	}
}

public class AddCardToDeckAction : GameActionBase
{
	public Card Card { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		Player player;
		if (context.Targets?.FirstOrDefault() is Player targetPlayer)
		{
			player = targetPlayer;
		}
		else
		{
			player = context.SourcePlayer;
		}

		reason = null;
		return player != null;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		if (!IsValid(state, actionContext, out var _)) { return []; }

		Player player;
		if (actionContext.Targets?.FirstOrDefault() is Player targetPlayer)
		{
			player = targetPlayer;
		}
		else
		{
			player = actionContext.SourcePlayer;
		}

		if (player != null)
		{
			player.Deck.Add(Card);
		}

		return [];
	}
}