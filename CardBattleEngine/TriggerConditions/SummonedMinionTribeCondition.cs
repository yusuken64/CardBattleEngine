namespace CardBattleEngine;

public class SummonedMinionTribeCondition : TriggerConditionBase
{
	public MinionTribe MinionTribe;
	public override bool Evaluate(EffectContext context)
	{
		if (context.SummonedUnit.Tribes.Count() == 0)
		{
			return false;
		}

		return context.SummonedUnit.Tribes.Contains(MinionTribe);
	}

	public override void ConsumeParams(Dictionary<string, object> actionParam)
	{
		object tribeData = actionParam[nameof(MinionTribe)];
		if (tribeData == null) { return; }

		MinionTribe = Enum.Parse<MinionTribe>(tribeData.ToString());
	}

	public override Dictionary<string, object> EmitParams()
	{
		return new Dictionary<string, object>
		{
			{ nameof(MinionTribe), MinionTribe.ToString() }
		};
	}
}
