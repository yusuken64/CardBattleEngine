namespace CardBattleEngine;

public class ContextSelector : AffectedEntitySelectorBase
{
	public bool IncludeTarget { get; set; }
	public bool IncludeSource { get; set; }
	public bool IncludeSourcePlayer { get; set; }
	public bool IncludeTargetOwner { get; set; }
	public bool IncludeSummonedMinion { get; set; }

	public override IEnumerable<IGameEntity> Select(GameState state, ActionContext context)
	{
		if (IncludeTarget && context.Target != null)
			yield return context.Target;

		if (IncludeSource && context.Source != null)
			yield return context.Source;

		if (IncludeSourcePlayer && context.SourcePlayer != null)
			yield return context.SourcePlayer;

		if (IncludeTargetOwner && context.Target?.Owner != null)
			yield return context.Target.Owner;

		if (IncludeSummonedMinion && context.SummonedMinion != null)
			yield return context.SummonedMinion;
	}
}