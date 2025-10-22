namespace CardBattleEngine.Events;

public class PlayerDamagedEvent : IGameEvent { public Player Player; public int Damage; public PlayerDamagedEvent(Player p, int d) { Player = p; Damage = d; } }
