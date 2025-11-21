namespace CardBattleEngine;

public class HeroPower
{
	public string Name { get; set; }
	public int ManaCost { get; set; }
	public bool UsedThisTurn { get; set; } = false;
	public TargetingType TargetingType { get; set; }
	public IAffectedEntitySelector AffectedEntitySelector { get; set; }
	public IEnumerable<IGameAction> GameActions { get; set; }
}
