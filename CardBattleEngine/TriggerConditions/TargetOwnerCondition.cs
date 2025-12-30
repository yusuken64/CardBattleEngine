namespace CardBattleEngine;

public class TargetOwnerCondition : TriggerConditionBase
{
	public TeamRelationship TeamRelationship { get; set; }

	public override bool Evaluate(ActionContext context)
	{
		IGameEntity entity = context.Target;

		switch (TeamRelationship)
		{
			case TeamRelationship.Enemy:
				return context.Source.Owner != entity.Owner;
				break;
			case TeamRelationship.Friendly:
				return context.Source.Owner == entity.Owner;
				break;
			case TeamRelationship.Any:
				return true;
				break;
		}

		return false;
	}
}
