namespace CardBattleEngine;

public class HealAction : GameActionBase
{
	public IValueProvider Amount { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.OnHealed;

	public override bool IsValid(GameState gameState, ActionContext context)
	{
		return context.Target.IsAlive &&
			context.Target.Health < context.Target.MaxHealth;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		int healAmount = Amount.GetValue(state, context);
		context.Target.Health = Math.Min(
			context.Target.Health + healAmount,
			context.Target.MaxHealth
		);

		context.HealedAmount = healAmount;
		return [];
	}
}