namespace CardBattleEngine;

public class ChangeKeywordAction : GameActionBase
{
	public ChangeType ChangeType { get; set; }
	public Keyword Keyword { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.None;

	public override bool IsValid(GameState state, ActionContext context, out string reason)
	{
		reason = null;
		return context.Target is Minion;
	}

	public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
	{
		if (context.Target is Minion minion)
		{
			var hasKeyword = ChangeType == ChangeType.Add;

			if (Keyword.HasFlag(Keyword.Stealth))
			{
				minion.IsStealth = hasKeyword;
			}
			if (Keyword.HasFlag(Keyword.Charge))
			{
				minion.HasCharge = hasKeyword;
			}
			if (Keyword.HasFlag(Keyword.DivineShield))
			{
				minion.HasDivineShield = hasKeyword;
			}
			if (Keyword.HasFlag(Keyword.Poisonous))
			{
				minion.HasPoisonous = hasKeyword;
			}
			if (Keyword.HasFlag(Keyword.Rush))
			{
				minion.HasRush = hasKeyword;
			}
			if (Keyword.HasFlag(Keyword.Windfury))
			{
				minion.HasWindfury = hasKeyword;
			}
			if (Keyword.HasFlag(Keyword.LifeSteal))
			{
				minion.HasLifeSteal = hasKeyword;
			}
			if (Keyword.HasFlag(Keyword.Reborn))
			{
				minion.HasReborn = hasKeyword;
			}
			if (Keyword.HasFlag(Keyword.Taunt))
			{
				minion.Taunt = hasKeyword;
			}
		}

		return [];
	}
}

public enum ChangeType
{
	Add,
	Remove
}

[Flags]
public enum Keyword
{
	None = 0,
	Stealth = 1 << 0,
	Charge = 1 << 1,
	DivineShield = 1 << 2,
	Poisonous = 1 << 3,
	Rush = 1 << 4,
	Windfury = 1 << 5,
	LifeSteal = 1 << 6,
	Reborn = 1 << 7,
	Taunt = 1 << 8,
}