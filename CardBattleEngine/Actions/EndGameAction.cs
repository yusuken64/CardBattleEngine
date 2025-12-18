namespace CardBattleEngine;

public class EndGameAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.GameEnd;

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		return true;
		//return gameState.IsGameOver();
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		// for the ui to transition to after game stuff
		// todo report end game stats?
		return [];
	}
}