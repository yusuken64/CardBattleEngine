namespace CardBattleEngine;

public class HeroPower
{
	public MinionCard LeaderCard { get; set; }

	public static HeroPower FromLeader(MinionCard leader)
	{
		var effect = leader?.MinionTriggeredEffects?.FirstOrDefault();
		if (effect?.EffectTrigger != EffectTrigger.Battlecry) return null;
		var selector = effect.AffectedEntitySelector;
		if (selector is ContextSelector context)
			selector = new ContextSelector
			{
				ResolutionTiming = context.ResolutionTiming,
				IncludeTarget = context.IncludeTarget, IncludeSource = context.IncludeSource,
				IncludeSourcePlayer = context.IncludeSourcePlayer || context.IncludeSummonedMinion,
				IncludeTargetOwner = context.IncludeTargetOwner,
				IncludeSummonedMinion = context.IncludeSummonedMinion,
			};
		return new HeroPower
		{
			Name = $"Invoke {leader.Name}", ManaCost = leader.ManaCost,
			LeaderCard = leader, ValidTargetSelector = leader.ValidTargetSelector,
			CastRestriction = leader.CastRestriction, AffectedEntitySelector = selector,
			GameActions = effect.GameActions.ToList(),
		};
	}

	public HeroPower Clone(Player owner)
	{
		var clone = (HeroPower)MemberwiseClone();
		clone.LeaderCard = (MinionCard)LeaderCard?.Clone();
		if (clone.LeaderCard != null) clone.LeaderCard.Owner = owner;
		return clone;
	}

	public string Name { get; set; }
	public int ManaCost { get; set; }
	public bool UsedThisTurn { get; set; } = false;
	public IValidTargetSelector? ValidTargetSelector { get; set; }
	public ICastRestriction? CastRestriction { get; set; }
	public IAffectedEntitySelector AffectedEntitySelector { get; set; }
	public IEnumerable<IGameAction> GameActions { get; set; }
}
