namespace CardBattleEngine;

public class PlayCardAction : GameActionBase
{
	public Card Card;
	public override EffectTrigger EffectTrigger => EffectTrigger.OnPlay;
	public Func<GameState, Player, TriggeredEffect, IEnumerable<IGameEntity>, Task<IGameEntity?>>? TargetSelector { get; set; }

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
			return [];

		Card.Owner.Mana -= Card.ManaCost;
		Card.Owner.Hand.Remove(Card);

		var effects = Card.GetPlayEffects(
			state,
			actionContext);

		return effects;
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