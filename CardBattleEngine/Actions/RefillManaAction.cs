namespace CardBattleEngine;

public class RefillManaAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState state, ActionContext actionContext, out string reason)
	{
		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		actionContext.SourcePlayer.Mana = actionContext.SourcePlayer.MaxMana;
		return [];
	}
}