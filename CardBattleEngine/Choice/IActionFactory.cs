namespace CardBattleEngine;

public interface IActionFactory
{
	IEnumerable<(IGameAction, ActionContext)> GetActions(Player sourcePlayer, IEnumerable<Card> list);
}
