namespace CardBattleEngine;

public class ReturnMinionToCard : GameActionBase
{
	public TeamRelationship TeamRelationship;
	public ZoneType ZoneType;

	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context)
	{
		if (context.Target is not Minion minion)
			return false;

		if (ZoneType != ZoneType.Hand ||
			ZoneType != ZoneType.Deck)
			return false;

		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (context.Target is Minion minion)
		{
			int index = minion.Owner.Board.IndexOf(minion);
			minion.Owner.Board.Remove(minion);

			Player owner = null;
			switch (TeamRelationship)
			{
				case TeamRelationship.Friendly:
					owner = minion.Owner;
					break;
				case TeamRelationship.Enemy:
					owner = state.OpponentOf(minion.Owner);
					break;
				case TeamRelationship.Any:
					owner = minion.Owner;
					break;
			}

			if (ZoneType == ZoneType.Hand)
			{
				(GainCardAction action, ActionContext context) gainCard =
					(new GainCardAction() { Card = minion.OriginalCard.Clone() },
					new ActionContext(context)
					{
						Target = owner
					});

				if (gainCard.action.IsValid(state, gainCard.context))
				{
					yield return gainCard;
				}
				else
				{
					yield return (new DeathAction(), context);
				}
			}
			else if (ZoneType == ZoneType.Deck)
			{
				(AddCardToDeckAction action, ActionContext context) addCard =
					(new AddCardToDeckAction { Card = minion.OriginalCard.Clone() },
					new ActionContext(context)
					{
						Target = owner
					});

				if (addCard.action.IsValid(state, addCard.context))
				{
					yield return addCard;
				}
				else
				{
					yield return (new DeathAction(), context);
				}
			}
		}
	}
}
