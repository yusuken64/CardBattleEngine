namespace CardBattleEngine;

public class Player : IGameEntity
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; }
	public List<Card> Deck { get; } = new List<Card>();
	public List<Card> Hand { get; } = new List<Card>();
	public List<Minion> Board { get; } = new List<Minion>();
	public List<Minion> Graveyard { get; } = new List<Minion>();
	public int CurrentMana { get; set; }
	public int MaxMana { get; set; }
	public int Attack { get; set; }
	public int Health { get; set; } = 30;
	public int Mana { get; set; }
	public bool CanAttack { get; internal set; }
	public bool HasAttacked { get; internal set; }

	public IAttackBehavior AttackBehavior => throw new NotImplementedException();

	public Player Owner { get; set; }

	public bool IsAlive { get; set; }
	public List<TriggeredEffect> TriggeredEffects { get; }
	public bool IsFrozen { get; internal set; }
	public bool HasAttackedThisTurn { get; internal set; }
	public bool MissedAttackFromFrozen { get; internal set; }
	public bool IsStealth { get; internal set; }

	public Player(string name) 
	{
		Name = name;
		IsAlive = true;
		Owner = this;
		TriggeredEffects = new();
	}

	public Player Clone()
	{
		var clone = new Player(Name)
		{
			CurrentMana = CurrentMana,
			MaxMana = MaxMana,
			Health = Health,
			Mana = Mana,
			CanAttack = CanAttack,
			HasAttacked = HasAttacked,
			IsAlive = IsAlive
		};

		// Deep copy the collections
		foreach (var card in Deck)
			clone.Deck.Add(card.Clone());

		foreach(var card in clone.Deck)
		{
			card.Owner = clone;
		}

		foreach (var card in Hand)
			clone.Hand.Add(card.Clone());

		foreach (var card in clone.Hand)
		{
			card.Owner = clone;
		}

		foreach (var minion in Board)
			clone.Board.Add(minion.Clone());

		foreach (var minion in clone.Board)
		{
			minion.Owner = clone;
		}

		foreach (var minion in Graveyard)
			clone.Graveyard.Add(minion.Clone());

		foreach (var minion in clone.Graveyard)
		{
			minion.Owner = clone;
		}

		return clone;
	}
}