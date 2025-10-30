namespace CardBattleEngine;

public class SpellOwnerCondition : TriggerConditionBase
{
	public TargetSide TargetSide { get; set; }
	public override bool Evaluate(EffectContext context)
	{
		switch (TargetSide)
		{
			case TargetSide.Enemy:
				return context.OriginalOwner != context.SecretOwner;
				break;
			case TargetSide.Friendly:
				return context.OriginalOwner != context.SecretOwner;
				break;
			case TargetSide.Both:
				return true;
				break;
		}

		return false;
	}
}