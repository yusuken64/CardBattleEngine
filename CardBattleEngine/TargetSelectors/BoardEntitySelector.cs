using System.Resources;
using System.Text.Json.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace CardBattleEngine;

public class BoardEntitySelector : AffectedEntitySelectorBase
{
	public TargetSide Side { get; set; } = TargetSide.Enemy; // Enemy, Friendly, Both
	public TargetGroup Group { get; set; } = TargetGroup.Minions; // Minions, Hero, All

	public override void ConsumeParams(Dictionary<string, object> actionParam)
	{
		if (actionParam.TryGetValue(nameof(Side), out var sideValue) && sideValue is string sideString)
		{
			Side = Enum.Parse<TargetSide>(sideString);
		}

		if (actionParam.TryGetValue(nameof(Group), out var groupValue) && groupValue is string groupString)
		{
			Group = Enum.Parse<TargetGroup>(groupString);
		}
	}

	public override Dictionary<string, object> EmitParams()
	{
		return new Dictionary<string, object>
		{
			{ nameof(Side), Side },
			{ nameof(Group), Group }
		};
	}

	public override IEnumerable<IGameEntity> Select(GameState state, ActionContext context)
	{
		var players = Side switch
		{
			TargetSide.Friendly => new[] { context.SourcePlayer },
			TargetSide.Enemy => new[] { state.OpponentOf(context.SourcePlayer) },
			TargetSide.Both => new[] { context.SourcePlayer, state.OpponentOf(context.SourcePlayer)},
			_ => Enumerable.Empty<Player>()
		};

		foreach (var player in players)
		{
			switch (Group)
			{
				case TargetGroup.Minions:
					foreach (var minion in player.Board)
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
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TargetSide
{
	Enemy,
	Friendly,
	Both
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TargetGroup
{
	Minions,
	Hero,
	All
}