namespace CardBattleEngine;

public class CombinedRestriction : ICastRestriction
{
	public ICastRestriction Left { get; set; }
	public CombinationOperation Operation { get; set; }
	public ICastRestriction Right { get; set; }

	public bool CanPlay(GameState gameState, Player player, Card castingCard, out string reason)
	{
		bool leftOk = Left.CanPlay(gameState, player, castingCard, out string leftReason);
		bool rightOk = Right.CanPlay(gameState, player, castingCard, out string rightReason);

		switch (Operation)
		{
			case CombinationOperation.And:
				if (leftOk && rightOk)
				{
					reason = "";
					return true;
				}

				reason = CombineReasons(leftOk, leftReason, rightOk, rightReason);
				return false;

			case CombinationOperation.Or:
				if (leftOk || rightOk)
				{
					reason = "";
					return true;
				}

				reason = CombineReasons(leftOk, leftReason, rightOk, rightReason);
				return false;

			case CombinationOperation.Except: // Left AND NOT Right
				if (!leftOk)
				{
					reason = leftReason;
					return false;
				}

				if (rightOk)
				{
					reason = $"Cannot play because exception rule is met: {rightReason}";
					return false;
				}

				reason = "";
				return true;

			default:
				throw new ArgumentOutOfRangeException();
		}
	}

	private string CombineReasons(bool leftOk, string leftReason, bool rightOk, string rightReason)
	{
		if (!leftOk && !rightOk)
			return $"{leftReason} AND {rightReason}";

		if (!leftOk)
			return leftReason;

		if (!rightOk)
			return rightReason;

		return "";
	}
}
