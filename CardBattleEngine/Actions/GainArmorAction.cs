namespace CardBattleEngine;

public class GainArmorAction : GameActionBase
{
	public IValueProvider Amount { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext actionContext)
	{
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		if (actionContext.Target is Player targetPlayer)
		{
			targetPlayer.Armor = Amount.GetValue(state, actionContext);
		}

		return [];
	}
}
