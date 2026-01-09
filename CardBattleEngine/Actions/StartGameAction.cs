namespace CardBattleEngine;

internal class StartGameAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.GameStart;

	public override bool IsValid(GameState state, ActionContext actionContext, out string reason)
	{
		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		if (!state.SkipShuffle)
		{
			state.Shuffle(actionContext.SourcePlayer.Deck);
		}

		for (int i = 0; i < state.InitialCards; i++)
		{
			yield return (new DrawCardFromDeckAction(), actionContext);
		}
	}
}
