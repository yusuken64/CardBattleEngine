namespace CardBattleEngine;

public class AmorValue : Value
{
	public TeamRelationship TeamRelationship;

	public override int GetValue(GameState state, ActionContext context)
	{
		Player player = null;
		switch (TeamRelationship)
		{
			case TeamRelationship.Friendly:
				player = context.Source.Owner;
				break;
			case TeamRelationship.Enemy:
				player =  state.OpponentOf(context.Source.Owner);
				break;
			case TeamRelationship.Any:
				player = context.Source.Owner;
				break;
		}

		return player == null ? 0 : player.Armor;
	}
}