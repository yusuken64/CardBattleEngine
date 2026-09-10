namespace CardBattleEngine;

public static class UntargetableFilter
{
    public static IEnumerable<IGameEntity> ExcludeUntargetable(IEnumerable<IGameEntity> candidates, Player picker)
    {
        return candidates.Where(e => !(e is Minion m && (m.IsStealth || m.Elusive) && m.Owner != picker));
    }
}
