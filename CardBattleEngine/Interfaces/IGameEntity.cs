namespace CardBattleEngine;

public interface IGameEntity
{
	Guid Id { get; set; }
	int Health { get; set; }
	int MaxHealth { get; set; }
	bool IsAlive { get; set; }
	public IAttackBehavior AttackBehavior { get; }
	int Attack { get; set; }
	Player Owner { get; set; }
	public bool CanAttack();
	public void AddModifier(StatModifier statModifier);
	public void AddAuraModifier(StatModifier statModifier);
	public void RemoveModifier(StatModifier modifier);
	public bool HasModifier(StatModifier modifier);
	public void ClearAuras(bool skipRecalculate);
	public void RecalculateStats();
	VariableSet VariableSet { get; }
}

public class VariableSet
{
	private readonly Dictionary<string, int> _vars = new();

	public void SetVar(string name, int value)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException(nameof(name));

		_vars[name] = value;
	}

	public int GetVarOrDefault(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException(nameof(name));

		return _vars.TryGetValue(name, out var value) ? value : 0;
	}

	public int GetRequiredVar(string name)
	{
		if (!_vars.TryGetValue(name, out var value))
			throw new InvalidOperationException($"Missing variable '{name}'");

		return value;
	}

	public bool HasVar(string name) => _vars.ContainsKey(name);
}