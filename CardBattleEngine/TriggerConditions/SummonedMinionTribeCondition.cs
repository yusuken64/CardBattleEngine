using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CardBattleEngine;
public class SummonedMinionTribeCondition : TriggerConditionBase
{
	public MinionToMinionRelationship MinionToMinionRelationship;
	public MinionTribe MinionTribe;
	public bool ExcludeSelf { get; set; } = false;
	public override bool Evaluate(EffectContext context)
	{
		var effectSource = context.EffectOwner;
		var summoned = context.SummonedUnit;

		if (summoned == null)
			return false;

		if (ExcludeSelf && summoned == effectSource)
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
		MinionTribe = Enum.Parse<MinionTribe>(actionParam[nameof(MinionTribe)].ToString(), true);
		MinionToMinionRelationship = Enum.Parse<MinionToMinionRelationship>(actionParam[nameof(MinionToMinionRelationship)].ToString(), true);
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

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MinionToMinionRelationship
{
	Friendly,
	Enemy,
	Any,
}