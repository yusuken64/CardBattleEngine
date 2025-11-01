

namespace CardBattleEngine;

public class Weapon : ITriggerSource
{
	public int Attack { get; set; }
	public int Durability { get; set; }

	public Player Owner { get; set; }

	public IEnumerable<TriggeredEffect> TriggeredEffects { get; set; }
}
