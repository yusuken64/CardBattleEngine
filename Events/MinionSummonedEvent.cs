namespace CardBattleEngine;

public class MinionSummonedEvent : IGameEvent { public Player Player; public Minion Minion; public MinionSummonedEvent(Player p, Minion m) { Player = p; Minion = m; } }
