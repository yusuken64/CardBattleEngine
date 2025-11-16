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
		// MinionTribe
		if (actionParam.TryGetValue(nameof(MinionTribe), out var tribeData) && tribeData != null)
		{
			MinionTribe = Enum.Parse<MinionTribe>(tribeData.ToString(), ignoreCase: true);
		}

		// MinionToMinionRelationship
		if (actionParam.TryGetValue(nameof(MinionToMinionRelationship), out var relationshipData) && relationshipData != null)
		{
			MinionToMinionRelationship = Enum.Parse<MinionToMinionRelationship>(relationshipData.ToString(), ignoreCase: true);
		}

		// ExcludeSelf (bool)
		if (actionParam.TryGetValue(nameof(ExcludeSelf), out var excludeData) && excludeData != null)
		{
			switch (excludeData)
			{
				case bool b:
					ExcludeSelf = b;
					break;
				case JValue jv when jv.Type == JTokenType.Boolean:
					ExcludeSelf = jv.Value<bool>();
					break;
				case string s when bool.TryParse(s, out bool parsed):
					ExcludeSelf = parsed;
					break;
				default:
					ExcludeSelf = false; // default
					break;
			}
		}
		else
		{
			ExcludeSelf = false; // default
		}
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