namespace CardBattleEngine;

public class HealAction : GameActionBase
{
	public int Amount;
	public override EffectTrigger EffectTrigger => EffectTrigger.OnHealed;

	public override bool IsValid(GameState gameState, ActionContext context)
	{
		return context.Target.IsAlive &&
			context.Target.Health < context.Target.MaxHealth;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		context.Target.Health = Math.Min(
			context.Target.Health + Amount,
			context.Target.MaxHealth
		);
		return [];
	}
}