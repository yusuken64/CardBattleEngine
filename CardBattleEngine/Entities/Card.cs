namespace CardBattleEngine;

public abstract class Card
{
	public Guid Id { get; } = Guid.NewGuid();
	public string Name { get; protected set; }
	public int ManaCost { get; protected set; }
	public abstract CardType Type { get; }
	public Player Owner { get; set; }
	public List<TriggeredEffect> TriggeredEffects { get; } = new();
	internal abstract IEnumerable<(IGameAction, ActionContext)> GetPlayEffects(GameState state, ActionContext actionContext);
	public abstract Card Clone();
}
