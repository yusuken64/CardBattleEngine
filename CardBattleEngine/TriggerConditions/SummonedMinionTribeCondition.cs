using System.Text.Json.Serialization;

namespace CardBattleEngine;
public class SummonedMinionTribeCondition : TriggerConditionBase
{
	public MinionToMinionRelationship MinionToMinionRelationship;
	public MinionTribe MinionTribe;
	public override bool Evaluate(EffectContext context)
	{
		var effectSource = context.EffectOwner;
		var summoned = context.SummonedUnit;

		if (summoned == null)
			return false;

		// Check tribe match
		if (summoned.Tribes == null || !summoned.Tribes.Contains(MinionTribe))
			return false;

		// Check relationship
		switch (MinionToMinionRelationship)
		{
			case MinionToMinionRelationship.Friendly:
				return effectSource.Owner == summoned.Owner;

			case MinionToMinionRelationship.Enemy:
				return effectSource.Owner != summoned.Owner;

			case MinionToMinionRelationship.Any:
				return true;

			default:
				return false;
		}
	}

	public override void ConsumeParams(Dictionary<string, object> actionParam)
	{
		object tribeData = actionParam[nameof(MinionTribe)];
		if (tribeData == null) { return; }

		object minionToMinionRelationshipData = actionParam[nameof(MinionToMinionRelationship)];
		if (minionToMinionRelationshipData == null) { return; }

		MinionTribe = Enum.Parse<MinionTribe>(tribeData.ToString());
		MinionToMinionRelationship = Enum.Parse<MinionToMinionRelationship>(minionToMinionRelationshipData.ToString());
	}

	public override Dictionary<string, object> EmitParams()
	{
		return new Dictionary<string, object>
		{
			{ nameof(MinionTribe), MinionTribe.ToString() },
			{ nameof(MinionToMinionRelationship), MinionToMinionRelationship.ToString() }
		};
	}
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MinionToMinionRelationship
{
	Friendly,
	Enemy,
	Any,
}