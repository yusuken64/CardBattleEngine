namespace CardBattleEngine;

public class SummonedMinionCondition : TriggerConditionBase
{
	public TeamRelationship MinionToMinionRelationship { get; set; }
	public string MinionTribe { get; set; }
	public bool ExcludeSelf { get; set; } = false;
	public override bool Evaluate(ActionContext context)
	{
		var effectSource = context.Source;
		var summoned = context.SummonedMinion;

		if (summoned == null)
			return false;

		if (ExcludeSelf && summoned == effectSource)
			return false;

		// An empty tribe filter means "any minion" - no tribe restriction.
		if (!string.IsNullOrEmpty(MinionTribe) &&
			!TribeUtils.Matches(summoned.Tribes, MinionTribe))
			return false;

		// Check relationship
		switch (MinionToMinionRelationship)
		{
			case TeamRelationship.Friendly:
				return effectSource.Owner == summoned.Owner;

			case TeamRelationship.Enemy:
				return effectSource.Owner != summoned.Owner;

			case TeamRelationship.Any:
				return true;

			default:
				return false;
		}
	}
}
