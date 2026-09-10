namespace CardBattleEngine;

public class SwapHealthAction : GameActionBase
{
    public override EffectTrigger EffectTrigger => EffectTrigger.None;

    public override bool IsValid(GameState state, ActionContext context, out string reason)
    {
        reason = null;
        return context.Targets != null &&
               context.Targets.Count == 2 &&
               context.Targets[0].IsAlive &&
               context.Targets[1].IsAlive;
    }

    public override IEnumerable<(IGameAction, ActionContext)> Resolve(GameState state, ActionContext context)
    {
        if (!IsValid(state, context, out _))
            yield break;

        var a = context.Targets[0];
        var b = context.Targets[1];

        int aHealth = a.Health;
        int bHealth = b.Health;

        a.Health = Utils.Clamp(bHealth, 0, a.MaxHealth);
        b.Health = Utils.Clamp(aHealth, 0, b.MaxHealth);

        if (a.Health <= 0)
            yield return (new DeathAction(), new ActionContext { SourcePlayer = context.SourcePlayer, Source = a, Targets = [a] });
        if (b.Health <= 0)
            yield return (new DeathAction(), new ActionContext { SourcePlayer = context.SourcePlayer, Source = b, Targets = [b] });
    }
}
