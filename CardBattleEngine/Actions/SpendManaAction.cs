namespace CardBattleEngine;

public class SpendManaAction : GameActionBase
{
	public int Amount { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext actionContext, out string reason)
	{
		reason = null;
		return true; //potentially need empty mana
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		actionContext.SourcePlayer.Mana =
			Math.Max(actionContext.SourcePlayer.Mana - Amount, 0);
		return [];
	}
}