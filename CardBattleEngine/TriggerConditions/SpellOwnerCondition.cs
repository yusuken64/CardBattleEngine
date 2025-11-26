namespace CardBattleEngine;

public class SpellOwnerCondition : TriggerConditionBase
{
	public TeamRelationship TeamRelationship { get; set; }
	public override bool Evaluate(ActionContext context)
	{
		switch (TeamRelationship)
		{
			case TeamRelationship.Enemy:
				return context.SourcePlayer != context.SourceCard.Owner;
				break;
			case TeamRelationship.Friendly:
				return context.SourcePlayer == context.SourceCard.Owner;
				break;
			case TeamRelationship.Any:
				return true;
				break;
		}

		return false;
	}
}