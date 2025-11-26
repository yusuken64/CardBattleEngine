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


	public override void ConsumeParams(Dictionary<string, object> actionParam)
	{
		MinionTribe = Utils.GetEnum<MinionTribe>(actionParam, nameof(MinionTribe));
		MinionToMinionRelationship = Utils.GetEnum<TeamRelationship>(actionParam, nameof(MinionToMinionRelationship));
		ExcludeSelf = actionParam.TryGetValue(nameof(ExcludeSelf), out var val) && val is bool b && b;
	}

	public override Dictionary<string, object> EmitParams()
	{
		return new Dictionary<string, object>
		{
			{ nameof(MinionTribe), MinionTribe.ToString() },
			{ nameof(MinionToMinionRelationship), MinionToMinionRelationship.ToString() },
			{ nameof(ExcludeSelf), ExcludeSelf.ToString() }
		};
	}
}
