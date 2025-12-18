namespace CardBattleEngine;

public class IncreaseMaxManaAction : GameActionBase
{
	public int Amount { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.None;
	public override bool IsValid(GameState state, ActionContext actionContext, out string reason)
	{
		reason = null;
		return actionContext.SourcePlayer.MaxMana < 10;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		actionContext.SourcePlayer.MaxMana = 
			Math.Min(actionContext.SourcePlayer.MaxMana + Amount, 10);
		return [];
	}
}
