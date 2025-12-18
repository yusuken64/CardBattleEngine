namespace CardBattleEngine;

public class RequestChoiceAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public IPendingChoice PendingChoice { get; set; }

	public override bool IsValid(GameState gameState, ActionContext context, out string reason)
	{
		reason = null;
		return gameState.CurrentPlayer == context.SourcePlayer;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		PendingChoice.SourcePlayer = context.SourcePlayer;
		PendingChoice.GetActions(state);
		state.PendingChoice = PendingChoice;

		return [];
	}
}