namespace CardBattleEngine;

public class TribeOperation : ITargetOperation
{
	public MinionTribe Tribe { get; set; }
	public bool ExcludeSelf { get; set; }

	public IEnumerable<IGameEntity> Apply(IEnumerable<IGameEntity> input, GameState state, ActionContext context)
	{
		if (input == null)
			yield break;

		foreach (var entity in input)
		{
			if (entity is Minion minion &&
				minion.Tribes.Contains(Tribe) &&
				(!ExcludeSelf || minion != context.Source))
			{
				yield return minion;
			}
		}
	}

	public void ConsumeParams(Dictionary<string, object> actionParam)
	{
		if (actionParam.TryGetValue(nameof(Tribe), out var tribeValue))
		{
			if (tribeValue is string tribeString)
			{
				if (Enum.TryParse<MinionTribe>(tribeString, ignoreCase: true, out var parsed))
					Tribe = parsed;
				else
					throw new ArgumentException($"Invalid MinionTribe value: '{tribeString}'");
			}
			else if (tribeValue is MinionTribe tribeEnum)
			{
				Tribe = tribeEnum;
			}
			else
			{
				throw new ArgumentException($"Tribe parameter must be a string or MinionTribe enum value.");
			}
		}

		if (actionParam.TryGetValue(nameof(ExcludeSelf), out var excludeValue))
		{
			if (excludeValue is bool excludeBool)
				ExcludeSelf = excludeBool;
			else if (excludeValue is string excludeStr && bool.TryParse(excludeStr, out var parsedBool))
				ExcludeSelf = parsedBool;
			else
				throw new ArgumentException($"ExcludeSelf parameter must be a bool or string 'true'/'false'.");
		}
	}

	public Dictionary<string, object> EmitParams()
	{
		return new Dictionary<string, object>
		{
			{ nameof(Tribe), Tribe.ToString() },
			{ nameof(ExcludeSelf), ExcludeSelf }
		};
	}
}
