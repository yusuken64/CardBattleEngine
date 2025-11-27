namespace CardBattleEngine;

public class FatigueAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState gameState, ActionContext context)
	{
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		context.SourcePlayer.Fatigue++;
		var actionContext = new ActionContext()
		{
			SourcePlayer = context.SourcePlayer,
			//Source = context.SourcePlayer,
			Target = context.SourcePlayer
		};
		yield return (new DamageAction() { Damage = (Value)context.SourcePlayer.Fatigue }, actionContext);
	}
}
