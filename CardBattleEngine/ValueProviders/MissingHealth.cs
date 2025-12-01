namespace CardBattleEngine;

public class MissingHealth : Value
{
	public TeamRelationship TeamRelationship { get; set; }

	public override int GetValue(GameState state, ActionContext context)
	{
		var player = TeamRelationship switch
		{
			TeamRelationship.Friendly => context.SourcePlayer,
			TeamRelationship.Enemy => state.OpponentOf(context.SourcePlayer),
			TeamRelationship.Any => context.SourcePlayer,
			_ => context.SourcePlayer
		};

		return Math.Max(0, player.MaxHealth - player.Health);
	}
}