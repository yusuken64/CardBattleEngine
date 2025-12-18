using CardBattleEngine;

namespace CardBattleEngine;

public class PlayCardAction : GameActionBase
{
	public Card Card; //TODO use card from context;
	public override EffectTrigger EffectTrigger => EffectTrigger.OnPlay;

	public override bool IsValid(GameState state, ActionContext actionContext, out string reason)
	{
		var player = Card.Owner;
		if (!player.Hand.Contains(Card))
		{
			reason = null;
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