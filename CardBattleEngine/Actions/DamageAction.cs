namespace CardBattleEngine;

public class DamageAction : GameActionBase
{
	public IGameEntity Target { get; set; }
	public int Damage { get; set; }
	public override EffectTrigger EffectTrigger => EffectTrigger.OnDamage;
	
	public override bool IsValid(GameState state)
	{
		// Valid if target is still alive / on board
		return Target != null && Target.IsAlive;
	}

	public override IEnumerable<GameActionBase> Resolve(GameState state, Player currentPlayer, Player opponent)
	{
		if (!IsValid(state))
			return [];

		// Apply damage
		Target.Health -= Damage;

		var sideEffects = new List<GameActionBase>();

		// Check for death triggers
		if (Target.Health <= 0)
		{
			sideEffects.Add(new DeathAction(Target));
		}

		return sideEffects;
	}

	public override void ConsumeParams(Dictionary<string, object> actionParam)
	{
		Damage = Convert.ToInt32(actionParam[nameof(Damage)]);
	}

	public override Dictionary<string, object> EmitParams()
	{
		return new Dictionary<string, object>
		{
			{ nameof(Damage), Damage }
		};
	}
}