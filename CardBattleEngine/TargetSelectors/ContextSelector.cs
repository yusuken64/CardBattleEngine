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
		if (IncludeTarget && context.Targets != null)
			foreach (var t in context.Targets)
				yield return t;

		if (IncludeSource && context.Source != null)
			yield return context.Source;

		if (IncludeSourcePlayer && context.SourcePlayer != null)
			yield return context.SourcePlayer;

		if (IncludeTargetOwner)
			foreach (var t in context.Targets ?? Enumerable.Empty<IGameEntity>())
				if (t.Owner != null)
					yield return t.Owner;

		if (IncludeSummonedMinion && context.SummonedMinion != null)
			yield return context.SummonedMinion;
	}
}