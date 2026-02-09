namespace CardBattleEngine;

public class AssignVariableAction : GameActionBase
{
	public string VariableName { get; set; }
	public IValueProvider Value { get; set; }
	public VariableScope VariableScope { get; set; }

	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState state, ActionContext context, out string reason)
	{
		if (string.IsNullOrWhiteSpace(VariableName))
		{
			reason = "Variable name is missing.";
			return false;
		}

		if (Value == null)
		{
			reason = "AssignVariableAction has no Value provider.";
			return false;
		}

		if (VariableScope == VariableScope.Entity && context.Source == null)
		{
			reason = $"Cannot assign variable '{VariableName}' to Entity scope because Source is null.";
			return false;
		}

		reason = null;
		return true;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(
		GameState state,
		ActionContext context)
	{
		switch (VariableScope)
		{
			case VariableScope.Context:
				context.SetVar(VariableName, Value.GetValueOrZero(state, context));
				break;
			case VariableScope.Entity:
				context.Source.VariableSet.SetVar(VariableName, Value.GetValueOrZero(state, context));
				break;
		}

		return [];
	}
}

public enum VariableScope
{
	Context,
	Entity,
	//Game
}