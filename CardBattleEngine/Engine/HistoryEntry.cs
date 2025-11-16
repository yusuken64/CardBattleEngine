namespace CardBattleEngine;

public class HistoryEntry
{
	public int Turn { get; set; }
	public Player Player { get; set; }
	public IGameAction Action { get; set; }
	public ActionContext Context { get; set; }
}