namespace CardBattleEngine;

public class SelectWeaponOperation : ITargetOperation
{
	public TeamRelationship Side { get; set; } = TeamRelationship.Enemy;
	public IEnumerable<IGameEntity> Apply(IEnumerable<IGameEntity> input, GameState state, ActionContext context)
	{
		var players = Side switch
		{
			TeamRelationship.Friendly => new[] { context.SourcePlayer },
			TeamRelationship.Enemy => new[] { state.OpponentOf(context.SourcePlayer) },
			TeamRelationship.Any => new[] { context.SourcePlayer, state.OpponentOf(context.SourcePlayer) },
			_ => Enumerable.Empty<Player>()
		};

		foreach(var player in players)
		{
			if (player.EquippedWeapon != null)
			{
				yield return player.EquippedWeapon;
			}
		}
	}

	public void ConsumeParams(Dictionary<string, object> actionParam)
	{
	}

	public Dictionary<string, object> EmitParams()
	{
		return [];
	}
}