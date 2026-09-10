namespace CardBattleEngine;

public class HealAction : GameActionBase
{
	public IValueProvider Amount { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.OnHealed;

	public override bool IsValid(GameState gameState, ActionContext actionContext, out string reason)
	{
		reason = null;
		var target = actionContext.Targets?.FirstOrDefault();
		return
			actionContext.AffectedEntitySelector != null ||
			(target != null &&
			 target.IsAlive &&
			 target.Health < target.MaxHealth);
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext actionContext)
	{
		var targets = this.ResolveTargets(state, actionContext);

		int healTotal = 0;
		foreach (var target in targets)
		{
			int healAmount = Amount.GetValue(state, actionContext);
			var originalHealth = target.Health;

			target.Health = Math.Min(
				target.Health + healAmount,
				target.MaxHealth
			);

			healTotal = target.Health - originalHealth;
			actionContext.AffectedEntities.Add((target, healTotal));
		}
		actionContext.HealedAmount = healTotal;
		return [];
	}
}