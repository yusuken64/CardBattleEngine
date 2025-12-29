namespace CardBattleEngine;

public abstract class Card : ITriggerSource, IGameEntity
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; protected set; }
	public string Description { get; set; }//doesn't affectgameplay
	public int ManaCost { get; protected set; }
	public abstract CardType Type { get; }
	public Player Owner { get; set; }
	internal abstract IEnumerable<(IGameAction, ActionContext)> GetPlayEffects(GameState state, ActionContext actionContext);
	public abstract Card Clone();

	#region ITriggerSource
	public IGameEntity Entity => this;

	public List<TriggeredEffect> TriggeredEffects { get; } = new List<TriggeredEffect>();

	#endregion

	#region IGameEntity
	Guid IGameEntity.Id { get => Id; set => throw new NotImplementedException(); }
	public abstract int Health { get; set; }
	public abstract int MaxHealth { get; set; }
	public abstract bool IsAlive { get; set; }
	public abstract IAttackBehavior AttackBehavior { get; }
	public abstract int Attack { get; set; }

	bool IGameEntity.CanAttack()
	{
		return false;
	}

	internal List<StatModifier> _modifiers = new();
	internal List<StatModifier> _auraModifiers = new();

	public void AddModifier(StatModifier modifier)
	{
		_modifiers.Add(modifier);
		RecalculateStats();
	}

	public void AddAuraModifier(StatModifier auraStatModifier)
	{
		_auraModifiers.Add(auraStatModifier);
		RecalculateStats();
	}

	public void RemoveModifier(StatModifier modifier)
	{
		_modifiers.Remove(modifier);
		RecalculateStats();
	}

	internal abstract void RecalculateStats();

	public bool HasModifier(StatModifier modifier)
	{
		return _modifiers.Contains(modifier);
	}

	public void ClearAuras()
	{
		_auraModifiers.Clear();
		RecalculateStats();
	}
	#endregion
}
