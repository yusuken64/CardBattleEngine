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
		return actionContext.SourcePlayer.Deck.Contains(actionContext.Target);
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		var card = actionContext.SourcePlayer.Deck.FirstOrDefault(x => x == actionContext.Target);
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
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		return context.SourcePlayer.Hand.Count() < 10;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		if (!IsValid(state, actionContext, out var _)) { return []; }

		actionContext.SourcePlayer.Hand.Add(Card);

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
		if (context.Target is Player targetPlayer)
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
		if (actionContext.Target is Player targetPlayer)
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