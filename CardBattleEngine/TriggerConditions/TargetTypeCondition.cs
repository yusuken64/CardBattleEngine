namespace CardBattleEngine;

public class TargetTypeCondition : TriggerConditionBase
{
	public EntityType EntityTypes { get; set; }  // note plural

	public override bool Evaluate(ActionContext context)
	{
		var target = context.Target;
		if (target == null) return false;

		return EntityTypeSelector.MatchesType(target, EntityTypes);
	}
}