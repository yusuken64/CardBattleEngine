namespace CardBattleEngine;

public class TargetSelectionChoice : IPendingChoice
{
	public Player SourcePlayer { get; set; }
	public IGameAction PendingAction { get; set; }
	public ActionContext PendingContext { get; set; }
	public TargetRequirement Requirement { get; set; }
	public List<IGameEntity> PickedSoFar { get; set; } = new();
	public IEnumerable<(IGameAction, ActionContext)> Options { get; set; }

	public IEnumerable<(IGameAction, ActionContext)> GetActions(GameState gameState)
	{
		if (Options != null) return Options;

		var candidates = Requirement.Provider.Select(gameState, SourcePlayer, PendingContext.SourceCard);
		candidates = UntargetableFilter.ExcludeUntargetable(candidates, SourcePlayer);

		if (!Requirement.AllowDuplicateTargets)
			candidates = candidates.Where(e => !PickedSoFar.Contains(e));

		Options = candidates.Select(entity => (
			(IGameAction)new SupplyTargetAction { Choice = this, Candidate = entity },
			new ActionContext { SourcePlayer = SourcePlayer, SourceCard = PendingContext.SourceCard }
		)).ToList();

		return Options;
	}
}
