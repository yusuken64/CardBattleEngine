namespace CardBattleEngine;

public class SourceOwnerCondition : TriggerConditionBase
{
	public TeamRelationship TeamRelationship { get; set; }
	public SourceType SourceType { get; set; }

	public override bool Evaluate(ActionContext context)
	{
		IGameEntity entity;
		switch (SourceType)
		{
			case SourceType.Card:
				entity = context.SourceCard?.Owner;
				break;
			case SourceType.Player:
				entity = context.OriginalSource?.Owner;
				break;
			case SourceType.Source:
				entity = context.Source?.Owner;
				break;
			case SourceType.TriggerSource:
				entity = context.Source;
				break;
			default:
				entity = context.Source;
				break;
		}
		switch (TeamRelationship)
		{
			case TeamRelationship.Enemy:
				return context.SourcePlayer != entity;
				break;
			case TeamRelationship.Friendly:
				return context.SourcePlayer == entity;
				break;
			case TeamRelationship.Any:
				return true;
				break;
		}

		return false;
	}
}

public enum SourceType
{
	Card,
	Player,
	Source,
	TriggerSource
}

public class OriginalSourceOwnerCondition : TriggerConditionBase
{
	public TeamRelationship TeamRelationship { get; set; }
	public override bool Evaluate(ActionContext context)
	{
		switch (TeamRelationship)
		{
			case TeamRelationship.Enemy:
				return context.Source.Owner != context.OriginalSource;
				break;
			case TeamRelationship.Friendly:
				return context.Source.Owner == context.OriginalSource;
				break;
			case TeamRelationship.Any:
				return true;
				break;
		}

		return false;
	}
}