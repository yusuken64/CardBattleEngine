namespace CardBattleEngine;

public class Sigil : ITriggerSource
{
	public List<TriggeredEffect> TriggeredEffects { get; set; } = new();
	public Player Owner { get; set; }
	public IGameEntity Entity => Owner;

	internal Sigil Clone()
	{
		return new Sigil
		{
			Owner = Owner,
			TriggeredEffects = TriggeredEffects.Select(e => e.Clone()).ToList(),
		};
	}
}
