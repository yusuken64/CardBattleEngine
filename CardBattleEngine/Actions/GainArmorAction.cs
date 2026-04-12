namespace CardBattleEngine;

public class GainArmorAction : GameActionBase
{
	public IValueProvider Amount { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext actionContext, out string reason)
	{
		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		var targets = ResolveTargets(state, actionContext);

		foreach (var target in targets)
		{
			if (target is Player targetPlayer)
			{
				int amount = Amount.GetValue(state, actionContext);
				targetPlayer.Armor += amount;
				actionContext.ArmorGained = amount;
			}
		}

		return [];
	}
}
