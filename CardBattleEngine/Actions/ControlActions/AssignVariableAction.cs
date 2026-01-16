namespace CardBattleEngine;

public class AssignVariableAction : GameActionBase
{
	public string VariableName { get; set; }
	public IValueProvider Value { get; set; }

	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState state, ActionContext context, out string reason)
	{
		reason = null;
		return !string.IsNullOrEmpty(VariableName);
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(
		GameState state,
		ActionContext context)
	{
		context.SetVar(VariableName, Value.GetValueOrZero(state, context));

		return [];
	}
}