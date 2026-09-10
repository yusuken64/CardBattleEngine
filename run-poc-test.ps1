# Run the POC test with piped input
# This script will drive the interactive game through both BodySwap (card-driven) and Sniper (trigger-driven) flows

# Build project first
Write-Host "Building CardBattleEngine.sln..." -ForegroundColor Cyan
dotnet build CardBattleEngine.sln -c Release 2>&1 | Out-Null

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Build successful. Running POC test..." -ForegroundColor Green
Write-Host ""

# Change to GameRunner directory
Push-Location "C:\Users\yusuke.hasegawa\source\repos\CardBattleEngine\GameRunner"

# Prepare input for the game:
# - We want to play BodySwap as the first action
# - Select first target (Big minion)
# - Select second target (Small minion)
# - Then play Sniper
# - Attack with Sniper (which will trigger the effect)
# - Select the Victim minion to take the damage
#
# The HumanAgent uses Console.ReadKey, so we need to send arrow keys and Enter
# Arrow Down = down, Arrow Up = up, Enter = select
# This is tricky with piped input, so we'll use a different approach

# Create a C# test driver that will programmatically drive the game
$testDriver = @"
using System;
using CardBattleEngine;
using GameRunner;

// Create game state
var gameState = GameFactory.CreateTestGame();
var engine = new GameEngine();
var player1 = gameState.Players[0];
var player2 = gameState.Players[1];

// Give players starting resources
player1.Mana = 10;
player2.Mana = 10;

// FIXTURE 1: BodySwap card
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

// Set up board
var big = new Minion(new MinionCard("Big", 1, 1, 10), player1);
var small = new Minion(new MinionCard("Small", 1, 1, 2), player1);
player1.Board.Add(big);
player1.Board.Add(small);

// FIXTURE 2: Sniper
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

// Set up opponent board
var bystander = new Minion(new MinionCard("Bystander", 1, 1, 5), player2);
var victim = new Minion(new MinionCard("Victim", 1, 1, 5), player2);
player2.Board.Add(bystander);
player2.Board.Add(victim);

engine.StartGame(gameState);

Console.WriteLine("=== FIXTURE TEST: BODYSWAP (CARD-DRIVEN) ===\n");

// ACTION 1: Play BodySwap
Console.WriteLine("Step 1: Playing BodySwap card...");
var playCardAction = gameState.GetValidActions(player1).First(a => a.Item1 is PlayCardAction pca && pca.Card == bodySwap);
engine.Resolve(gameState, playCardAction.Item2, playCardAction.Item1);

Console.WriteLine("Check: PendingChoice should be a TargetSelectionChoice");
if (gameState.PendingChoice is TargetSelectionChoice)
{
    Console.WriteLine("✓ PASS: PendingChoice is TargetSelectionChoice");
}
else
{
    Console.WriteLine($"✗ FAIL: PendingChoice is {gameState.PendingChoice?.GetType().Name}");
}

// Get valid actions for target selection
var validTargets = gameState.GetValidActions(player1);
Console.WriteLine($"Available targets after playing BodySwap: {validTargets.Count(a => a.Item1 is SupplyTargetAction)}");

// ACTION 2: Pick first target (Big)
var pickBig = gameState.GetValidActions(player1).First(a => ((SupplyTargetAction)a.Item1).Candidate == big);
engine.Resolve(gameState, pickBig.Item2, pickBig.Item1);
Console.WriteLine("Picked Big as first target");

// ACTION 3: Pick second target (Small)
var remainingTargets = gameState.GetValidActions(player1).Where(a => a.Item1 is SupplyTargetAction).ToList();
Console.WriteLine($"Remaining targets after first pick: {remainingTargets.Count}");

if (remainingTargets.Count == 1)
{
    Console.WriteLine("✓ PASS: Only 1 target remaining (Big was excluded after first pick)");
}
else
{
    Console.WriteLine($"✗ FAIL: Expected 1 target, got {remainingTargets.Count}");
}

var pickSmall = gameState.GetValidActions(player1).First(a => ((SupplyTargetAction)a.Item1).Candidate == small);
engine.Resolve(gameState, pickSmall.Item2, pickSmall.Item1);
Console.WriteLine("Picked Small as second target");

// Check health swap result
Console.WriteLine($"\nHealth after swap:");
Console.WriteLine($"  Big: {big.Health} (expected 2)");
Console.WriteLine($"  Small: {small.Health} (expected 2)");

if (big.Health == 2 && small.Health == 2)
{
    Console.WriteLine("✓ PASS: Health values correctly swapped and clamped");
}
else
{
    Console.WriteLine("✗ FAIL: Health swap did not work correctly");
}

Console.WriteLine("\n=== FIXTURE TEST: SNIPER (TRIGGER-DRIVEN) ===\n");

// Skip opponent's turn (they pass or do nothing)
Console.WriteLine("Skipping to Player 1's next turn...");
if (gameState.CurrentPlayer == player2)
{
    var endTurn = gameState.GetValidActions(player2).First();
    engine.Resolve(gameState, endTurn.Item2, endTurn.Item1);
}

// Play Sniper
Console.WriteLine("Step 1: Playing Sniper card...");
var playSniperAction = gameState.GetValidActions(player1).First(a => a.Item1 is PlayCardAction pca && pca.Card == sniperCard);
engine.Resolve(gameState, playSniperAction.Item2, playSniperAction.Item1);

var sniper = (Minion)player1.Board.FirstOrDefault(m => m.Card.Name == "Sniper");

// Attack with Sniper
Console.WriteLine("Step 2: Attacking with Sniper...");
sniper.HasSummoningSickness = false;

var attackAction = gameState.GetValidActions(player1).First(a => a.Item1 is AttackAction);
var attackContext = new ActionContext() { Source = sniper, SourcePlayer = player1, Targets = new List<IGameEntity> { player2 } };
engine.Resolve(gameState, attackContext, new AttackAction());

Console.WriteLine("Check: PendingChoice should exist after attack trigger");
if (gameState.PendingChoice is TargetSelectionChoice)
{
    Console.WriteLine("✓ PASS: Attack trigger created a TargetSelectionChoice");
}
else
{
    Console.WriteLine($"✗ FAIL: Expected TargetSelectionChoice, got {gameState.PendingChoice?.GetType().Name}");
}

// Check available targets
var triggerTargets = gameState.GetValidActions(player1).Where(a => a.Item1 is SupplyTargetAction).ToList();
Console.WriteLine($"Available targets in trigger prompt: {triggerTargets.Count}");

if (triggerTargets.Count == 2)
{
    Console.WriteLine("✓ PASS: Both defending minions are valid targets");
}
else
{
    Console.WriteLine($"✗ FAIL: Expected 2 targets, got {triggerTargets.Count}");
}

// Pick victim for damage
var pickVictim = gameState.GetValidActions(player1).First(a => ((SupplyTargetAction)a.Item1).Candidate == victim);
engine.Resolve(gameState, pickVictim.Item2, pickVictim.Item1);
Console.WriteLine("Picked Victim for trigger damage");

// Check damage application
Console.WriteLine($"\nHealth after trigger damage:");
Console.WriteLine($"  Victim: {victim.Health} (expected 2, took 3 damage)");
Console.WriteLine($"  Bystander: {bystander.Health} (expected 5, untouched)");

if (victim.Health == 2 && bystander.Health == 5)
{
    Console.WriteLine("✓ PASS: Trigger damage correctly applied only to selected target");
}
else
{
    Console.WriteLine("✗ FAIL: Trigger damage did not work correctly");
}

Console.WriteLine("\n=== TESTS COMPLETE ===");
"@

# For now, just run the program directly with the flag
Write-Host "Launching interactive POC test (--test-poc mode)..." -ForegroundColor Cyan
Write-Host "Instructions: Play BodySwap targeting Big then Small, then play Sniper and attack with it" -ForegroundColor Yellow
Write-Host ""

& dotnet run -- --test-poc

Pop-Location
