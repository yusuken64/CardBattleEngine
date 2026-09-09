namespace CardBattleEngine;

// Most actions (PlayCardAction, SubmitMulliganAction) carry enough of their own fields to describe
// themselves via ToString(). AttackAction doesn't - attacker/target live on the paired ActionContext,
// not the action instance - so any UI listing legal actions (local console, networked LegalActionView)
// needs both to produce a useful description.
public static class ActionDisplay
{
	public static string Describe(IGameAction action, ActionContext context)
	{
		if (action is AttackAction)
		{
			return $"Attack: {DescribeEntity(context.Source)} -> {DescribeEntity(context.Target)}";
		}

		return action.ToString();
	}

	private static string DescribeEntity(IGameEntity entity)
	{
		return entity switch
		{
			Player player => $"{player.Name} (Hero) {player.Attack}/{player.Health}",
			Minion minion => $"{minion.Name} {minion.Attack}/{minion.Health}",
			_ => entity?.ToString() ?? "?",
		};
	}
}
