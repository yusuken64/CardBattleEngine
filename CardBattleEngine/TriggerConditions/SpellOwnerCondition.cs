namespace CardBattleEngine;

public class SpellOwnerCondition : TriggerConditionBase
{
	public TargetSide TargetSide { get; set; }
	public override bool Evaluate(ActionContext context)
	{
		switch (TargetSide)
		{
			case TargetSide.Enemy:
				return context.SourcePlayer != context.SourceCard.Owner;
				break;
			case TargetSide.Friendly:
				return context.SourcePlayer == context.SourceCard.Owner;
				break;
			case TargetSide.Both:
				return true;
				break;
		}

		return false;
	}
}