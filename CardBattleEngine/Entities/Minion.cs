namespace CardBattleEngine;

public class Minion : IGameEntity
{
	private MinionCard card;
	private Player owner;

	public Guid Id { get; } = Guid.NewGuid();
	public string TemplateName { get; }
	public int Attack { get; set; }
	public int Health { get; set; }
	public Player Owner => owner;
	public bool Taunt { get; internal set; }
	public bool HasSummoningSickness { get; internal set; }
	public bool HasAttackedThisTurn { get; internal set; }

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
		this.owner = owner;

		Attack = card.Attack;
		Health = card.Health;

		_attackBehavior = new MinionAttackBehavior();
		HasAttackedThisTurn = false;
		HasSummoningSickness = true;
		IsAlive = true;
	}

	public bool CanAttack()
	{
		return !HasSummoningSickness && !HasAttackedThisTurn;
	}
}