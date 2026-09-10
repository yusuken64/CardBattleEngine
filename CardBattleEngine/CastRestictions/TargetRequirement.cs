namespace CardBattleEngine;

public class TargetRequirement
{
    public IValidTargetSelector Provider { get; set; }
    public int Count { get; set; } = 1;
    public bool AllowDuplicateTargets { get; set; } = false;
}
