namespace CardBattleEngine;

public abstract class Card
{
	public Guid Id { get; } = Guid.NewGuid();
	public string Name { get; protected set; }
	public int ManaCost { get; protected set; }
	public CardType Type { get; protected set; }
	public Player Owner { get; set; }
	public List<TriggeredEffect> TriggeredEffect { get; } = new();

	// Every card knows how to generate its effects when played
	internal abstract IEnumerable<IGameAction> GetPlayEffects(GameState state, Player currentPlayer, Player opponent);

	public abstract Card Clone();
}