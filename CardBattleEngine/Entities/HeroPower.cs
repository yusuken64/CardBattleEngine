namespace CardBattleEngine;

public class HeroPower
{
	public string Name { get; set; }
	public int ManaCost { get; set; }
	public bool UsedThisTurn { get; set; } = false;
	public IValidTargetSelector? ValidTargetSelector { get; set; }
	public ICastRestriction? CastRestriction { get; set; }
	public IAffectedEntitySelector AffectedEntitySelector { get; set; }
	public IEnumerable<IGameAction> GameActions { get; set; }
}
