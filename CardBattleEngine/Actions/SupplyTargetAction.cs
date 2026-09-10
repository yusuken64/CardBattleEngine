namespace CardBattleEngine;

public class SupplyTargetAction : GameActionBase
{
    public TargetSelectionChoice Choice { get; set; }
    public IGameEntity Candidate { get; set; }
    public override EffectTrigger EffectTrigger => EffectTrigger.None;

    public override bool IsValid(GameState state, ActionContext context, out string reason)
    {
        reason = null;
        return state.CurrentPlayer == context.SourcePlayer && Choice != null && Candidate != null;
    }

    public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
    {
        var picked = new List<IGameEntity>(Choice.PickedSoFar) { Candidate };

        if (picked.Count >= Choice.Requirement.Count)
        {
            yield return (Choice.PendingAction, new ActionContext(Choice.PendingContext) { Targets = picked });
        }
        else
        {
            state.PendingChoice = new TargetSelectionChoice
            {
                SourcePlayer = Choice.SourcePlayer,
                PendingAction = Choice.PendingAction,
                PendingContext = Choice.PendingContext,
                Requirement = Choice.Requirement,
                PickedSoFar = picked,
            };
        }
    }

    public override string ToString() => $"Pick target: {Candidate}";
}
