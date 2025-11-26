
namespace CardBattleEngine;

public class Secret : ITriggerSource
{
	public TriggeredEffect SecretTrigger { get; set; }
	public Player Owner { get; set; }
	public IGameEntity Entity => Owner;
	public IEnumerable<TriggeredEffect> TriggeredEffects => [SecretTrigger];
}