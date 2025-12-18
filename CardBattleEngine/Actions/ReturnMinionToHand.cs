namespace CardBattleEngine;

public class ReturnMinionToCard : GameActionBase
{
	public TeamRelationship TeamRelationship;
	public ZoneType ZoneType;

	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		if (context.Target is not Minion minion)
		{
			reason = null;
			return false;
		}

		if (ZoneType != ZoneType.Hand ||
			ZoneType != ZoneType.Deck)
		{
			reason = null;
			return false;
		}

		reason = null;
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

				if (gainCard.action.IsValid(state, gainCard.context, out string _))
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

				if (addCard.action.IsValid(state, addCard.context, out var _))
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
