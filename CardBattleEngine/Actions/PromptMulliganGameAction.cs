using System;
using System.Collections.Generic;
using System.Text;

namespace CardBattleEngine.Actions;

public class PromptMulliganGameAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context)
	{
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

		Options = [(new SubmitMulliganAction(), new ActionContext() { SourcePlayer = SourcePlayer })];

		return Options;
	}
}

public class SubmitMulliganAction : GameActionBase
{
	public List<Card> CardsToReplace { get; set; } = new();
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context)
	{
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
		yield return (new StartTurnAction(), context);
	}
}