namespace CardBattleEngine.Events;

public class SpellCastEvent : IGameEvent { public Player Player; public SpellCastEvent(Player p) { Player = p; } }
