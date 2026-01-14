
namespace CardBattleEngine;

public abstract class ControlActionBase : GameActionBase
{
	public List<IGameAction> ChildActions { get; set; }
}
