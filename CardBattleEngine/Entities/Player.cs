namespace CardBattleEngine;

public class Player : IGameEntity
{
	public Guid Id { get; } = Guid.NewGuid();
	public string Name { get; set; }
	public List<Card> Deck { get; } = new List<Card>();
	public List<Card> Hand { get; } = new List<Card>();
	public List<Minion> Board { get; } = new List<Minion>();
	public List<Minion> Graveyard { get; } = new List<Minion>();
	public int CurrentMana { get; set; }
	public int MaxMana { get; set; }
	public int Health { get; set; } = 30;
	public int Mana { get; internal set; }
	public bool CanAttack { get; internal set; }
	public bool HasAttacked { get; internal set; }

	public IAttackBehavior AttackBehavior => throw new NotImplementedException();

	public int Attack => throw new NotImplementedException();

	public Player Owner => this;

	public bool IsAlive { get; set; }

	public Player(string name) 
	{
		Name = name;
		IsAlive = true;
	}
}