namespace CardBattleEngine.Events;

public class MinionAttackedEvent : IGameEvent { public Minion Attacker; public Minion Defender; public MinionAttackedEvent(Minion a, Minion d) { Attacker = a; Defender = d; } }
