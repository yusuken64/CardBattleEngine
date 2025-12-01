namespace CardBattleEngine;

public class ContextSelector : AffectedEntitySelectorBase
{
	public bool IncludeTarget { get; set; }
	public bool IncludeSource { get; set; }
	public bool IncludeSourcePlayer { get; set; }
	public bool IncludeTargetOwner { get; set; }

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
	}

	public override Dictionary<string, object> EmitParams()
	{
		var ret = new Dictionary<string, object>();

		ret[nameof(IncludeTarget)] = IncludeTarget;
		ret[nameof(IncludeSource)] = IncludeSource;
		ret[nameof(IncludeSourcePlayer)] = IncludeSourcePlayer;
		ret[nameof(IncludeTargetOwner)] = IncludeTargetOwner;

		return ret;
	}

	public override void ConsumeParams(Dictionary<string, object> p)
	{
		IncludeTarget = p.TryGetValue("IncludeTarget", out var t) && (bool)t;
		IncludeSource = p.TryGetValue("IncludeSource", out var s) && (bool)s;
		IncludeSourcePlayer = p.TryGetValue("IncludeSourcePlayer", out var sp) && (bool)sp;
		IncludeTargetOwner = p.TryGetValue("IncludeTargetOwner", out var to) && (bool)to;
	}
}