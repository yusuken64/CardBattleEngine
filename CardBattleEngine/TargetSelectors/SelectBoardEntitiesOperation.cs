namespace CardBattleEngine;
public class SelectBoardEntitiesOperation : ITargetOperation
{
	public TeamRelationship Side { get; set; } = TeamRelationship.Enemy;   // Enemy, Friendly, Both
	public TargetGroup Group { get; set; } = TargetGroup.Minions; // Minions, Hero, All
	public bool ExcludeSelf { get; set; }
	public IEnumerable<IGameEntity> Apply(IEnumerable<IGameEntity> input, GameState state, ActionContext context)
	{
		// Ignore the input — this is the "base selection"
		var players = Side switch
		{
			TeamRelationship.Friendly => new[] { context.SourcePlayer },
			TeamRelationship.Enemy => new[] { state.OpponentOf(context.SourcePlayer) },
			TeamRelationship.Any => new[] { context.SourcePlayer, state.OpponentOf(context.SourcePlayer) },
			_ => Enumerable.Empty<Player>()
		};

		foreach (var player in players)
		{
			switch (Group)
			{
				case TargetGroup.Minions:
					foreach (var minion in player.Board)
						if (!ExcludeSelf || minion != context.Source)
							yield return minion;
					break;
				case TargetGroup.Hero:
					yield return player;
					break;
				case TargetGroup.All:
					foreach (var minion in player.Board)
						yield return minion;
					yield return player;
					break;
			}
		}
	}

	// Optional: serialization
	public Dictionary<string, object> EmitParams()
	{
		return new Dictionary<string, object>
		{
			{ nameof(Side), Side.ToString() },
			{ nameof(Group), Group.ToString() }
		};
	}

	public void ConsumeParams(Dictionary<string, object> actionParam)
	{
		Side = JsonParamHelper.GetEnum<TeamRelationship>(actionParam, nameof(Side), Side);
		Group = JsonParamHelper.GetEnum<TargetGroup>(actionParam, nameof(Group), Group);
	}
}