namespace CardBattleEngine;

public class RandomOperation : ITargetOperation
{
	public IValueProvider Count { get; set; }

	public IEnumerable<IGameEntity> Apply(IEnumerable<IGameEntity> input, GameState state, ActionContext context)
	{
		return state.ChooseRandom([.. input], Count.GetValue(state, context));
	}

	public void ConsumeParams(Dictionary<string, object> actionParam)
	{
	}

	public Dictionary<string, object> EmitParams()
	{
		return null;
	}
}
