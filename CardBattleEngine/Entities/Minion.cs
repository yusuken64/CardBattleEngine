
namespace CardBattleEngine;

public class Minion : IGameEntity
{
	private MinionCard card;
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; set; }
	public string TemplateName { get; set; }
	public int Attack { get; set; }
	public int Health { get; set; }
	public Player Owner { get; set; }
	public bool Taunt { get; set; }
	public bool HasSummoningSickness { get; internal set; }
	public bool HasAttackedThisTurn { get; internal set; }
	public List<TriggeredEffect> TriggeredEffects { get; }

	private IAttackBehavior _attackBehavior;
	IAttackBehavior IGameEntity.AttackBehavior
	{
		get
		{
			return _attackBehavior;
		}
	}

	public bool IsAlive{ get; set; }

	public Minion(MinionCard card, Player owner)
	{
		this.card = card;
		Owner = owner;

		Attack = card.Attack;
		Health = card.Health;

		_attackBehavior = new MinionAttackBehavior();
		HasAttackedThisTurn = false;
		HasSummoningSickness = true;
		IsAlive = true;
		TriggeredEffects = new();

		foreach (var effect in card.TriggeredEffect)
		{
			var instance = effect.CloneFor(this);
			TriggeredEffects.Add(instance);
		}
	}

	public bool CanAttack()
	{
		return !HasSummoningSickness && !HasAttackedThisTurn;
	}

	internal Minion Clone()
	{
		return new Minion(this.card, Owner)
		{
			Id = this.Id,
			TemplateName = this.TemplateName,
			Attack = this.Attack,
			Health = this.Health,
			Owner = this.Owner,
			Taunt = this.Taunt,
			HasSummoningSickness = this.HasSummoningSickness,
			HasAttackedThisTurn = this.HasAttackedThisTurn
		};
	}
}