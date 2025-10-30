
namespace CardBattleEngine;

public class Secret : ITriggerSource
{
	public TriggeredEffect SecretTrigger { get; set; }
	public Player Owner { get; set; }
	public IEnumerable<TriggeredEffect> TriggeredEffects => [SecretTrigger];
}