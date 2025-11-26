namespace CardBattleEngine.ValueProviders;

public class StatValue : Value
{
	public Stat EntityStat;
	public ContextProvider EntityContextProvider;

	public override int GetValue(GameState state, ActionContext context)
	{
		IGameEntity entity = context.Target;
		switch (EntityContextProvider)
		{
			case ContextProvider.Target:
				entity = context.Target;
				break;
			case ContextProvider.Source:
				entity = context.Source;
				break;
		}

		switch (EntityStat)
		{
			case Stat.Cost:
				return entity.Attack;
				break;
			case Stat.Attack:
				return entity.Attack;
				break;
			case Stat.Health:
				return entity.Health;
				break;
		}

		return 0;
	}

	public enum Stat
	{
		Cost,
		Attack,
		Health
	}

	public enum ContextProvider
	{
		Target,
		Source
	}
}