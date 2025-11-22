using CardBattleEngine;

namespace CardBattleEngine;

public class PlayCardAction : GameActionBase
{
	public Card Card; //TODO use card from context;
	public override EffectTrigger EffectTrigger => EffectTrigger.OnPlay;

	public override bool IsValid(GameState state, ActionContext actionContext)
	{
		var player = Card.Owner;
		if (!player.Hand.Contains(Card))
			return false;

		if (Card.ManaCost > player.Mana)
			return false;

		if (Card is MinionCard minionCard && player.Board.Count >= state.MaxBoardSize)
			return false;

		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		if (!IsValid(state, actionContext))
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