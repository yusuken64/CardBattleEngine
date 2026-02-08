namespace CardBattleEngine;

public class PlayCardAction : GameActionBase
{
	public Card Card; //TODO use card from context;
	public override EffectTrigger EffectTrigger => EffectTrigger.OnPlay;

	public override bool IsValid(GameState state, ActionContext actionContext, out string reason)
	{
		var canCast = CanCast(state, actionContext, out reason);
		if (!canCast)
		{
			return false;
		}

		var validTargets = Card.ValidTargetSelector?.Select(state, actionContext.SourcePlayer, Card);
		if (validTargets != null &&
			validTargets.Any() &&
			!validTargets.Contains(actionContext.Target))
		{
			reason = "Invalid Target";
			return false;
		}

		reason = null;
		return true;
	}

	public bool CanCast(GameState state, ActionContext actionContext, out string reason)
	{
		var player = Card.Owner;
		if (!player.Hand.Contains(Card))
		{
			reason = "Invalid Card";
			return false;
		}

		if (Card.CastRestriction != null &&
			!Card.CastRestriction.CanPlay(
				state,
				actionContext.SourcePlayer,
				Card,
				out reason))
		{
			return false;
		}

		if (Card.ManaCost > player.Mana)
		{
			reason = "Not Enough Mana";
			return false;
		}

		if (Card is MinionCard minionCard && player.Board.Count >= state.MaxBoardSize)
		{
			reason = "Board is Full";
			return false;
		}

		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		if (!IsValid(state, actionContext, out string _))
			yield break;

		yield return (new SpendManaAction { Amount = Card.ManaCost }, actionContext);

		Card.Owner.Hand.Remove(Card);

		foreach (var effect in Card.GetPlayEffects(state, actionContext))
			yield return effect;
	}

	public override string ToString()
	{
		if (Card is MinionCard minionCard)
		{
			return $"Playcard {Card.Name} ({Card.ManaCost}){minionCard.Attack}/{minionCard.Health}";
		}

		return base.ToString();
	}
}