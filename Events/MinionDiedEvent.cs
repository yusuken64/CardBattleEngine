namespace CardBattleEngine;

public class MinionDiedEvent : IGameEvent { public Player Owner; public Minion Minion; public MinionDiedEvent(Player o, Minion m) { Owner = o; Minion = m; } }