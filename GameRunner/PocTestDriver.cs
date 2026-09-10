using CardBattleEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GameRunner;

/// <summary>
/// Automated driver for the POC tests - exercises both card-driven and trigger-driven multi-target flows.
/// Captures and displays the ACTUAL ActionDisplay text that HumanAgent would render to the player.
/// </summary>
public class PocTestDriver
{
	public static void RunTests()
	{
		Console.WriteLine("=== MULTI-TARGET POC TEST DRIVER ===");
		Console.WriteLine("(Capturing actual ActionDisplay text for menu inspection)\n");

		// Create game state
		var gameState = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player1 = gameState.Players[0];
		var player2 = gameState.Players[1];

		// Give players starting resources
		player1.Mana = 10;
		player2.Mana = 10;

		// FIXTURE 1: BodySwap card (card-driven multi-target selection)
		Console.WriteLine("Setting up BodySwap fixture (card-driven)...");
		var bodySwap = new SpellCard("BodySwap", 2)
		{
			RequiredTargetCount = 2,
			ValidTargetSelector = new EntityTypeSelector
			{
				EntityTypes = EntityType.Minion,
				TeamRelationship = TeamRelationship.Any
			},
		};
		bodySwap.SpellCastEffects.Add(new SpellCastEffect
		{
			GameActions = new List<IGameAction> { new SwapHealthAction() }
		});
		player1.Hand.Add(bodySwap);
		bodySwap.Owner = player1;

		// Set up player1's board with minions for the BodySwap swap test
		var big = new Minion(new MinionCard("Big", 1, 1, 10), player1);
		var small = new Minion(new MinionCard("Small", 1, 1, 2), player1);
		player1.Board.Add(big);
		player1.Board.Add(small);

		// FIXTURE 2: Sniper minion (trigger-driven multi-target selection)
		Console.WriteLine("Setting up Sniper fixture (trigger-driven)...\n");
		var sniperCard = new MinionCard("Sniper", 3, 2, 3)
		{
			Owner = player1,
			HasCharge = true,
		};
		sniperCard.MinionTriggeredEffects.Add(new TriggeredEffect
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
		player1.Hand.Add(sniperCard);

		// Set up player2's board with minions
		var bystander = new Minion(new MinionCard("Bystander", 1, 1, 5), player2);
		var victim = new Minion(new MinionCard("Victim", 1, 1, 5), player2);
		player2.Board.Add(bystander);
		player2.Board.Add(victim);

		engine.StartGame(gameState);

		// Skip mulligan phase - submit mulligan for both players
		Console.WriteLine("Completing mulligan phase...");
		while (gameState.PendingChoice != null)
		{
			var currentActions = gameState.GetValidActions(gameState.CurrentPlayer).ToList();
			var submitAction = currentActions.FirstOrDefault(a => a.Item1 is SubmitMulliganAction);
			if (submitAction != default)
			{
				engine.Resolve(gameState, submitAction.Item2, submitAction.Item1);
			}
			else
			{
				break;
			}
		}
		Console.WriteLine("Mulligan complete.\n");

		Console.WriteLine("========== FIXTURE 1: BODYSWAP (CARD-DRIVEN) ==========\n");

		// ACTION 1: Play BodySwap
		Console.WriteLine("[ACTION] Playing BodySwap card...");
		var validActions = gameState.GetValidActions(player1).ToList();
		var playCardAction = validActions.FirstOrDefault(a => a.Item1 is PlayCardAction pca && pca.Card == bodySwap);
		if (playCardAction == default)
		{
			Console.WriteLine("✗ FAIL: Could not find PlayCardAction for BodySwap");
			return;
		}

		// Display what HumanAgent would show for the card action menu
		Console.WriteLine("\n[MENU TEXT] Available actions (what HumanAgent shows):");
		foreach (var action in validActions)
		{
			var displayText = ActionDisplay.Describe(action.Item1, action.Item2);
			Console.WriteLine($"  - {displayText}");
		}

		engine.Resolve(gameState, playCardAction.Item2, playCardAction.Item1);

		Console.WriteLine("\n[CHECK] BodySwap card played - checking for TargetSelectionChoice...");
		if (gameState.PendingChoice is TargetSelectionChoice)
		{
			Console.WriteLine("✓ PASS: PendingChoice is TargetSelectionChoice");
		}
		else
		{
			Console.WriteLine($"✗ FAIL: PendingChoice is {gameState.PendingChoice?.GetType().Name}, expected TargetSelectionChoice");
		}

		// Get valid actions for target selection
		var firstTargetOptions = gameState.GetValidActions(player1).Where(a => a.Item1 is SupplyTargetAction).ToList();
		Console.WriteLine($"\n[MENU TEXT] First target selection menu ({firstTargetOptions.Count} targets available):");
		foreach (var targetAction in firstTargetOptions)
		{
			var displayText = ActionDisplay.Describe(targetAction.Item1, targetAction.Item2);
			Console.WriteLine($"  - {displayText}");
		}

		Console.WriteLine($"\n[CHECK] {firstTargetOptions.Count} targets available (expected 2: Big and Small)");
		if (firstTargetOptions.Count == 2)
		{
			Console.WriteLine("✓ PASS: Both minions available as targets");
		}
		else
		{
			Console.WriteLine($"✗ FAIL: Expected 2 targets, got {firstTargetOptions.Count}");
		}

		// ACTION 2: Pick first target (Big)
		Console.WriteLine("\n[ACTION] Selecting first target: Big");
		var pickBig = gameState.GetValidActions(player1).First(a => ((SupplyTargetAction)a.Item1).Candidate == big);
		engine.Resolve(gameState, pickBig.Item2, pickBig.Item1);
		Console.WriteLine("✓ Big selected as first target");

		// ACTION 3: Pick second target (Small)
		var secondTargetOptions = gameState.GetValidActions(player1).Where(a => a.Item1 is SupplyTargetAction).ToList();

		Console.WriteLine($"\n[MENU TEXT] Second target selection menu ({secondTargetOptions.Count} targets available):");
		foreach (var targetAction in secondTargetOptions)
		{
			var displayText = ActionDisplay.Describe(targetAction.Item1, targetAction.Item2);
			Console.WriteLine($"  - {displayText}");
		}

		Console.WriteLine($"\n[CHECK] {secondTargetOptions.Count} targets available (expected 1: Small only)");
		if (secondTargetOptions.Count == 1)
		{
			Console.WriteLine("✓ PASS: Big was excluded after first pick - only Small remains");
		}
		else
		{
			Console.WriteLine($"✗ FAIL: Expected 1 target after exclusion, got {secondTargetOptions.Count}");
		}

		Console.WriteLine("\n[ACTION] Selecting second target: Small");
		var pickSmall = gameState.GetValidActions(player1).First(a => ((SupplyTargetAction)a.Item1).Candidate == small);
		engine.Resolve(gameState, pickSmall.Item2, pickSmall.Item1);
		Console.WriteLine("✓ Small selected as second target");

		// Check health swap result
		Console.WriteLine("\n[CHECK] Verifying health swap result:");
		Console.WriteLine($"  Big health: {big.Health} (original 10, should be 2 after swap)");
		Console.WriteLine($"  Small health: {small.Health} (original 2, should be 2 after swap due to max health clamp)");

		if (big.Health == 2 && small.Health == 2)
		{
			Console.WriteLine("✓ PASS: Health values correctly swapped and clamped to MaxHealth");
		}
		else
		{
			Console.WriteLine($"✗ FAIL: Health swap incorrect - Big: {big.Health} (expected 2), Small: {small.Health} (expected 2)");
		}

		Console.WriteLine("\n========== FIXTURE 2: SNIPER (TRIGGER-DRIVEN) ==========\n");

		// Advance to next turn
		Console.WriteLine("[ACTION] Advancing to next turn (Player 2's turn)...");
		if (gameState.CurrentPlayer == player1)
		{
			var endTurn = gameState.GetValidActions(player1).First(a => a.Item1 is EndTurnAction);
			engine.Resolve(gameState, endTurn.Item2, endTurn.Item1);
		}

		// Player 2 ends their turn
		Console.WriteLine("[ACTION] Player 2 ends turn...");
		if (gameState.CurrentPlayer == player2)
		{
			var endTurn = gameState.GetValidActions(player2).First(a => a.Item1 is EndTurnAction);
			engine.Resolve(gameState, endTurn.Item2, endTurn.Item1);
		}

		// Play Sniper
		Console.WriteLine("[ACTION] Playing Sniper card...");
		var playSniperAction = gameState.GetValidActions(player1).First(a => a.Item1 is PlayCardAction pca && pca.Card == sniperCard);
		engine.Resolve(gameState, playSniperAction.Item2, playSniperAction.Item1);
		Console.WriteLine("✓ Sniper played");

		// Find the sniper minion on the board
		var sniper = (Minion)player1.Board.FirstOrDefault(m => m.Name == "Sniper");
		if (sniper == null)
		{
			Console.WriteLine("✗ FAIL: Sniper not found on board after playing");
			return;
		}

		// Remove summoning sickness so it can attack
		sniper.HasSummoningSickness = false;

		// Attack with Sniper - this will trigger the post-attack effect with TargetRequirement
		Console.WriteLine("\n[ACTION] Attacking with Sniper (targeting hero)...");
		var attackAction = gameState.GetValidActions(player1).First(a => a.Item1 is AttackAction);
		var attackContext = new ActionContext()
		{
			Source = sniper,
			SourcePlayer = player1,
			Targets = new List<IGameEntity> { player2 }
		};
		engine.Resolve(gameState, attackContext, new AttackAction());
		Console.WriteLine("✓ Attack resolved");

		// Check for pending choice from the trigger
		Console.WriteLine("\n[CHECK] Verifying trigger-driven target selection...");
		if (gameState.PendingChoice is TargetSelectionChoice)
		{
			Console.WriteLine("✓ PASS: Attack trigger created a TargetSelectionChoice (trigger-driven selection active)");
		}
		else
		{
			Console.WriteLine($"✗ FAIL: Expected TargetSelectionChoice after trigger, got {gameState.PendingChoice?.GetType().Name}");
		}

		// Check available targets - both minions should be options
		var triggerTargetOptions = gameState.GetValidActions(player1).Where(a => a.Item1 is SupplyTargetAction).ToList();

		Console.WriteLine($"\n[MENU TEXT] Trigger-driven target selection menu ({triggerTargetOptions.Count} targets available):");
		Console.WriteLine("(Note: This uses the same ActionDisplay.Describe() path as the card-driven case above)");
		foreach (var targetAction in triggerTargetOptions)
		{
			var displayText = ActionDisplay.Describe(targetAction.Item1, targetAction.Item2);
			Console.WriteLine($"  - {displayText}");
		}

		var targetCandidates = triggerTargetOptions.Select(a => ((SupplyTargetAction)a.Item1).Candidate).ToList();
		var hasBystander = targetCandidates.Contains(bystander);
		var hasVictim = targetCandidates.Contains(victim);

		Console.WriteLine($"\n[CHECK] {triggerTargetOptions.Count} targets available (expected 2: Bystander and Victim)");
		if (triggerTargetOptions.Count == 2 && hasBystander && hasVictim)
		{
			Console.WriteLine("✓ PASS: Both enemy minions are valid candidates in trigger prompt (no restriction to attacked minion)");
		}
		else
		{
			Console.WriteLine($"✗ FAIL: Expected both minions, got {triggerTargetOptions.Count} options");
			if (!hasBystander) Console.WriteLine("  - Bystander missing");
			if (!hasVictim) Console.WriteLine("  - Victim missing");
		}

		// Pick victim for damage
		Console.WriteLine("\n[ACTION] Selecting target for trigger damage: Victim");
		var pickVictim = gameState.GetValidActions(player1).First(a => ((SupplyTargetAction)a.Item1).Candidate == victim);
		engine.Resolve(gameState, pickVictim.Item2, pickVictim.Item1);
		Console.WriteLine("✓ Victim selected");

		// Check damage application
		Console.WriteLine("\n[CHECK] Verifying trigger damage application:");
		Console.WriteLine($"  Victim health: {victim.Health} (original 5, should be 2 after 3 damage)");
		Console.WriteLine($"  Bystander health: {bystander.Health} (original 5, should remain 5 - untouched)");

		if (victim.Health == 2 && bystander.Health == 5)
		{
			Console.WriteLine("✓ PASS: Trigger damage correctly applied only to selected target");
		}
		else
		{
			Console.WriteLine($"✗ FAIL: Trigger damage incorrect - Victim: {victim.Health} (expected 2), Bystander: {bystander.Health} (expected 5)");
		}

		Console.WriteLine("\n========== ALL TESTS COMPLETE ==========\n");
	}
}
