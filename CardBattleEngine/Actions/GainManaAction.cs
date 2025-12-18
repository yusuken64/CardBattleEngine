namespace CardBattleEngine;

public class GainManaAction : GameActionBase
{
	public IValueProvider Amount { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext actionContext, out string reason)
	{
		reason = null;
		return true; //potentially need empty mana
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		actionContext.SourcePlayer.Mana = 
			Math.Min(actionContext.SourcePlayer.Mana + Amount.GetValue(state, actionContext), 10);
		return [];
	}
}
