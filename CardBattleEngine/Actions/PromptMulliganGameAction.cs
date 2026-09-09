namespace CardBattleEngine;

public class PromptMulliganGameAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		state.PendingChoice = new MulliganChoce()
		{
			SourcePlayer = context.SourcePlayer
		};
		yield break;
	}
}

internal class MulliganChoce : IPendingChoice
{
	public Player SourcePlayer { get; set; }
	public IEnumerable<(IGameAction, ActionContext)> Options { get; set; }

	public IEnumerable<(IGameAction, ActionContext)> GetActions(GameState gameState)
	{
		if (Options != null) { return Options; }

		// One option per possible subset of the starting hand to replace, matching how every other
		// choice point in the engine (e.g. AttackAction enumerating one option per attacker/target
		// pair) hands out a fully-specified action per legal combination rather than needing a
		// separate multi-select interaction. Mulligan hands are small (3-4 cards), so 2^n options is
		// cheap.
		var hand = SourcePlayer.Hand;
		var options = new List<(IGameAction, ActionContext)>();

		for (int mask = 0; mask < (1 << hand.Count); mask++)
		{
			var cardsToReplace = new List<Card>();
			for (int i = 0; i < hand.Count; i++)
			{
				if ((mask & (1 << i)) != 0)
				{
					cardsToReplace.Add(hand[i]);
				}
			}

			options.Add((
				new SubmitMulliganAction { CardsToReplace = cardsToReplace },
				new ActionContext { SourcePlayer = SourcePlayer }));
		}

		Options = options;
		return Options;
	}
}

public class SubmitMulliganAction : GameActionBase
{
	public List<Card> CardsToReplace { get; set; } = new();
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		foreach(var card in CardsToReplace)
		{
			context.SourcePlayer.Hand.Remove(card);
			yield return (new AddCardToDeckAction() { Card = card }, context);
		}

		foreach (var _ in CardsToReplace)
		{
			yield return (new DrawCardFromDeckAction(), context);
		}
		state.Shuffle(context.SourcePlayer.Deck);
		if (state.PendingMulligans != null && state.PendingMulligans.Count > 0)
		{
			var nextPlayer = state.PendingMulligans.Dequeue();
			yield return (new PromptMulliganGameAction(), new ActionContext { SourcePlayer = nextPlayer });
		}
		else
		{
			yield return (new StartTurnAction(), new ActionContext { SourcePlayer = state.Players[0] });
		}
	}

	public override string ToString()
	{
		return CardsToReplace.Count == 0
			? "Keep all"
			: "Mulligan: " + string.Join(", ", CardsToReplace.Select(c => c.Name));
	}
}