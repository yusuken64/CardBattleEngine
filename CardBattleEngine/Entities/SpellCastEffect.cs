namespace CardBattleEngine;

public class SpellCastEffect
{
	public IAffectedEntitySelector AffectedEntitySelector { get; set; }
	public List<IGameAction> GameActions { get; set; } = new();
}
