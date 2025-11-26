

namespace CardBattleEngine;

public class Weapon : ITriggerSource
{
	public string Name { get; set; }
	public int Attack { get; set; }
	public int Durability { get; set; }

	public Player Owner { get; set; }
	public IGameEntity Entity => Owner;

	public IEnumerable<TriggeredEffect> TriggeredEffects { get; set; }
}
