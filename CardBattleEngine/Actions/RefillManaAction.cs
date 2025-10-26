namespace CardBattleEngine;

public class RefillManaAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState state, ActionContext actionContext) => true;

	public override IEnumerable<GameActionBase> Resolve(GameState state, ActionContext actionContext)
	{
		actionContext.SourcePlayer.Mana = actionContext.SourcePlayer.MaxMana;
		return [];
	}
}