namespace CardBattleEngine;

internal class DeathAction : GameActionBase
{
	public override EffectTrigger EffectTrigger => EffectTrigger.OnDeath;

	public override bool IsValid(GameState state, ActionContext actionContext)
	{
		return actionContext.Target.IsAlive;
	}

	public override IEnumerable<IGameAction> Resolve(GameState state, ActionContext actionContext)
	{
		var sideEffects = new List<IGameAction>();
		actionContext.Target.IsAlive = false;
		if (actionContext.Target is Minion minion)
		{
			minion.Owner.Board.Remove(minion);
			minion.Owner.Graveyard.Add(minion);

			foreach (var effect in minion.TriggeredEffects
				.Where(x => x.EffectTrigger == EffectTrigger.Deathrattle))
			{
				sideEffects.AddRange(effect.GameActions);
			}
		}

		return sideEffects;
	}
}