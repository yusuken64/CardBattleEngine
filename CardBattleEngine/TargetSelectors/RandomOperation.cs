namespace CardBattleEngine;

public class RandomOperation : ITargetOperation
{
	public IValueProvider Count { get; set; }

	public IEnumerable<IGameEntity> Apply(IEnumerable<IGameEntity> input, GameState state, ActionContext context)
	{
		return state.ChooseRandom([.. input], Count.GetValue(state, context));
	}
}
