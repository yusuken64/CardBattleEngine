namespace CardBattleEngine;

public class CombinedCondition : TriggerConditionBase
{
	public ITriggerCondition Left { get; set; }
	public CombinationOperation Operation { get; set; }
	public ITriggerCondition Right { get; set; }

	public override bool Evaluate(ActionContext context)
	{
		bool leftOk = Left.Evaluate(context);
		bool rightOk = Right.Evaluate(context);

		switch (Operation)
		{
			case CombinationOperation.And:
				return leftOk && rightOk;
				break;
			case CombinationOperation.Or:
				return leftOk || rightOk;
				break;
			case CombinationOperation.Except:
				break;
		}

		return false;
	}
}
