namespace CardBattleEngine;

public class SpellCastEffect
{
	public TargetType TargetType { get; set; }
	public List<IGameAction> GameActions { get; set; } = new();
}
