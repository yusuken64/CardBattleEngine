namespace CardBattleEngine.Test;

/// <summary>
/// Test for attack-triggered abilities with target selection.
/// Proves that the generic targeting mechanism handles attack triggers the same way it handles card plays.
/// </summary>
[TestClass]
public class AttackTriggerTargetTest
{
	[TestMethod]
	public void AttackTriggerSelectMinionDealsThreeDamage()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var attackerOwner = state.CurrentPlayer;
		var defenderOwner = state.OpponentOf(attackerOwner);

		// Create the attacker minion with a triggered effect that fires after attacking
		var attackerCard = new MinionCard("Sniper", 3, 2, 3)
		{
			Owner = attackerOwner,
			HasCharge = true,
		};

		// Add the post-attack trigger that requires target selection
		attackerCard.MinionTriggeredEffects.Add(new TriggeredEffect
		{
			EffectTrigger = EffectTrigger.Attack,
			EffectTiming = EffectTiming.Post,
			Scope = TriggerScope.Self,
			TargetRequirement = new TargetRequirement
			{
				Provider = new EntityTypeSelector
				{
					EntityTypes = EntityType.Minion,
					TeamRelationship = TeamRelationship.Enemy
				},
				Count = 1,
			},
			GameActions = new List<IGameAction> { new DamageAction { Damage = (Value)3 } },
		});

		// Play the attacker minion
		attackerOwner.Mana = 3;
		attackerOwner.Hand.Add(attackerCard);
		engine.Resolve(state,
			new ActionContext()
			{
				SourcePlayer = attackerOwner,
				SourceCard = attackerCard,
			},
			new PlayCardAction() { Card = attackerCard });

		var attacker = (Minion)attackerOwner.Board[0];
		// Clear summoning sickness so the Charge minion can attack immediately
		attacker.HasSummoningSickness = false;

		// Create defending minions
		var bystanderCard = new MinionCard("Bystander", 1, 1, 5)
		{
			Owner = defenderOwner,
		};
		var bystander = new Minion(bystanderCard, defenderOwner);
		defenderOwner.Board.Add(bystander);

		var victimCard = new MinionCard("Victim", 1, 1, 5)
		{
			Owner = defenderOwner,
		};
		var victim = new Minion(victimCard, defenderOwner);
		defenderOwner.Board.Add(victim);

		// Drive the attack against the hero
		// This will trigger the post-attack ability with TargetRequirement
		var attackContext = new ActionContext()
		{
			Source = attacker,
			SourcePlayer = attackerOwner,
			Targets = new List<IGameEntity> { defenderOwner },
		};
		engine.Resolve(state, attackContext, new AttackAction());

		// The post-attack trigger's DamageAction now has a PendingTargetRequirement and no Targets yet,
		// so GameEngine.Resolve's central interception should have paused here.
		Assert.IsNotNull(state.PendingChoice, "Should have a pending choice after attack trigger");
		Assert.IsInstanceOfType(state.PendingChoice, typeof(TargetSelectionChoice),
			"PendingChoice should be a TargetSelectionChoice");

		// Get valid actions - should be SupplyTargetAction options for each valid target
		var validActions = state.GetValidActions(attackerOwner);
		var supplyActions = validActions
			.Where(a => a.Item1 is SupplyTargetAction)
			.ToList();

		// Both defending minions should be valid candidates (the trigger's TargetRequirement doesn't restrict to "the defender")
		Assert.AreEqual(2, supplyActions.Count,
			"Should have 2 supply target options (both defending minions are valid)");

		var candidates = supplyActions
			.Select(a => ((SupplyTargetAction)a.Item1).Candidate)
			.ToList();
		Assert.IsTrue(candidates.Contains(bystander),
			"Bystander should be a valid target candidate");
		Assert.IsTrue(candidates.Contains(victim),
			"Victim should be a valid target candidate");

		// Pick the victim for the damage
		var pickAction = supplyActions
			.First(a => ((SupplyTargetAction)a.Item1).Candidate == victim);
		engine.Resolve(state, pickAction.Item2, pickAction.Item1);

		// Verify the results
		Assert.IsNull(state.PendingChoice, "PendingChoice should be cleared after target is selected");
		Assert.AreEqual(2, victim.Health, "Victim should take 3 trigger damage (5 - 3 = 2)");
		Assert.AreEqual(5, bystander.Health, "Bystander should be untouched — the player chose victim, not bystander");
	}
}
