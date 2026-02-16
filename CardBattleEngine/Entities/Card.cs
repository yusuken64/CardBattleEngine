namespace CardBattleEngine;

public abstract class Card : ITriggerSource, IGameEntity
{
	public Guid Id { get; set; } = Guid.NewGuid();
	public string Name { get; protected set; }
	public string SpriteID { get; set; }//doesn't affectgameplay
	public string Description { get; set; }//doesn't affectgameplay
	public ushort NumericId { get; internal set; }//used for shorthand id of card and effects
	public int ManaCost { get; protected set; }
	public IValidTargetSelector? ValidTargetSelector { get; set; }
	public ICastRestriction? CastRestriction { get; set; }
	public abstract CardType Type { get; }
	public Player Owner { get; set; }
	internal abstract IEnumerable<(IGameAction, ActionContext)> GetPlayEffects(GameState state, ActionContext actionContext);
	public abstract Card Clone();

	#region ITriggerSource
	public IGameEntity Entity => this;

	public List<TriggeredEffect> TriggeredEffects { get; internal set; } = new List<TriggeredEffect>();

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
		//RecalculateStats();
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

	public void ClearAuras(bool skipRecalculate)
	{
		_auraModifiers.Clear();

		if (!skipRecalculate)
		{
			RecalculateStats();
		}
	}

	void IGameEntity.RecalculateStats()
	{
		RecalculateStats();
	}

	#endregion
	public VariableSet VariableSet { get; set; } = new();
}
