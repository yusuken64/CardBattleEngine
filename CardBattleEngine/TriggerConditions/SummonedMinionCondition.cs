namespace CardBattleEngine;

public class SummonedMinionCondition : TriggerConditionBase
{
	public TeamRelationship MinionToMinionRelationship { get; set; }
	public MinionTribe MinionTribe { get; set; }
	public bool ExcludeSelf { get; set; } = false;
	public override bool Evaluate(ActionContext context)
	{
		var effectSource = context.Source;
		var summoned = context.SummonedMinion;

		if (summoned == null)
			return false;

		if (ExcludeSelf && summoned == effectSource)
			return false;

		if (MinionTribe != MinionTribe.None &&
			MinionTribe != MinionTribe.All)
		{
			// Check tribe match
			if (summoned.Tribes == null ||
				!summoned.Tribes.Contains(MinionTribe))
				return false;
		}

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
