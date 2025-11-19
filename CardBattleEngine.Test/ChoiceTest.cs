namespace CardBattleEngine.Test;

[TestClass]
public class ChoiceTest
{
	[TestMethod]
	public void DiscoverTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine(new XorShiftRNG(1));

		var current = state.CurrentPlayer;
		current.Mana = 1;
		var opponent = state.OpponentOf(current);

		var discoverCard = new SpellCard("DiscoverCard", cost: 1);
		current.Hand.Add(discoverCard);
		discoverCard.Owner = current;
		discoverCard.SpellCastEffects.Add(new SpellCastEffect()
		{
			GameActions = [new RequestChoiceAction()
			{
				PendingChoice = new SimpleChoice(){
					Options =
					[
						(new SummonMinionAction()
						{
							Card = new MinionCard("Choice1", 1, 1, 3),
						}, new()),
						(new SummonMinionAction()
						{
							Card = new MinionCard("Choice2", 1, 3, 1),
						}, new()),
					]
				}
			}]
		});

		ActionContext actionContext = new()
		{
			SourcePlayer = current
		};
		PlayCardAction playCardAction = new PlayCardAction()
		{
			Card = discoverCard
		};
		engine.Resolve(state, actionContext, playCardAction);

		Assert.IsNotNull(state.PendingChoice);
		state.GetValidActions(current);
		var choices = state.GetValidActions(current).Select(c => c.Item1).ToList();
		var expected = ((SimpleChoice)state.PendingChoice).Options.Select(o => o.Item1).ToList();
		CollectionAssert.AreEquivalent(expected, choices);
	}

	[TestMethod]
	public void DiscoverChoiceIsExecuted()
	{
		// Arrange: create game state and engine
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine(new XorShiftRNG(1));
		var current = state.CurrentPlayer;
		current.Mana = 1;

		// Discover card setup
		var discoverCard = new SpellCard("DiscoverCard", cost: 1);
		discoverCard.Owner = current;
		current.Hand.Add(discoverCard);
		discoverCard.SpellCastEffects.Add(new SpellCastEffect
		{
			GameActions = new List<IGameAction>
			{
				new RequestChoiceAction
				{
					PendingChoice = new SimpleChoice
					{
						Options = new List<(IGameAction, ActionContext)>
						{
							(new SummonMinionAction
							{
								Card = new MinionCard("Choice1", 1, 1, 3)
							}, new ActionContext()
							{
								SourcePlayer = current,
							}),
							(new SummonMinionAction
							{
								Card = new MinionCard("Choice2", 1, 3, 1)
							}, new ActionContext(){
								SourcePlayer = current,
							})
						}
					}
				}
			}
		});

		var playCardCtx = new ActionContext { SourcePlayer = current };
		var playCardAction = new PlayCardAction { Card = discoverCard };

		// Act Step 1: Play the card (engine sets PendingChoice)
		engine.Resolve(state, playCardCtx, playCardAction);

		// Assert pending choice exists
		Assert.IsNotNull(state.PendingChoice);
		var validActions = state.GetValidActions(current);

		// Act Step 2: Simulate picking the first choice
		var selected = validActions[0];
		engine.Resolve(state, selected.Item2, selected.Item1);

		// Assert the choice executed
		var minionOnBoard = current.Board.FirstOrDefault(m => m.Name == "Choice1");
		Assert.IsNotNull(minionOnBoard, "Chosen minion should be on the board");

		// Assert PendingChoice is cleared after resolution
		Assert.IsNull(state.PendingChoice, "PendingChoice should be cleared after execution");
	}

	[TestMethod]
	public void PendingChoiceRejectsInvalidAction()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine(new XorShiftRNG(1));
		var current = state.CurrentPlayer;
		current.Mana = 1;

		// Create discover card
		var discoverCard = new SpellCard("DiscoverCard", cost: 1) { Owner = current };
		current.Hand.Add(discoverCard);

		discoverCard.SpellCastEffects.Add(new SpellCastEffect()
		{
			GameActions =
			[
				new RequestChoiceAction()
			{
				PendingChoice = new SimpleChoice()
				{
					Options =
					[
						(new SummonMinionAction()
						{
							Card = new MinionCard("Choice1", 1, 1, 3)
						}, new ActionContext(){ SourcePlayer = current }),

						(new SummonMinionAction()
						{
							Card = new MinionCard("Choice2", 1, 3, 1)
						}, new ActionContext(){ SourcePlayer = current })
					]
				}
			}
			]
		});

		// Act 1: Play the card to trigger the choice
		engine.Resolve(state, new ActionContext() { SourcePlayer = current }, new PlayCardAction() { Card = discoverCard });

		Assert.IsNotNull(state.PendingChoice, "PendingChoice should be set");

		// Create an INVALID action that is not one of the options
		var invalidAction = new SummonMinionAction()
		{
			Card = new MinionCard("INVALID", 99, 99, 99)
		};
		var invalidCtx = new ActionContext() { SourcePlayer = current };

		// Act 2: Try to resolve invalid action
		engine.Resolve(state, invalidCtx, invalidAction);

		// Assert: Nothing should have happened
		Assert.IsNotNull(state.PendingChoice, "Invalid action must NOT clear PendingChoice");
		Assert.AreEqual(0, current.Board.Count, "Invalid choice should not summon anything");
	}

	[TestMethod]
	public void OpponentCannotResolvePendingChoice()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine(new XorShiftRNG(1));
		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);

		current.Mana = 1;

		// Discover card
		var discoverCard = new SpellCard("DiscoverCard", cost: 1) { Owner = current };
		current.Hand.Add(discoverCard);

		discoverCard.SpellCastEffects.Add(new SpellCastEffect()
		{
			GameActions =
			[
				new RequestChoiceAction()
			{
				PendingChoice = new SimpleChoice()
				{
					Options =
					[
						(new SummonMinionAction()
						{
							Card = new MinionCard("Choice1", 1, 1, 3)
						}, new ActionContext(){ SourcePlayer = current }),

						(new SummonMinionAction()
						{
							Card = new MinionCard("Choice2", 1, 3, 1)
						}, new ActionContext(){ SourcePlayer = current })
					]
				}
			}
			]
		});

		// Act 1: Play the card
		engine.Resolve(state, new ActionContext() { SourcePlayer = current }, new PlayCardAction() { Card = discoverCard });

		Assert.IsNotNull(state.PendingChoice);

		// Opponent tries to pick choice 0
		var validOptions = state.GetValidActions(current); // Current player's options
		var (opAction, opCtxTemplate) = validOptions[0];

		var opponentCtx = new ActionContext()
		{
			SourcePlayer = opponent // INVALID source
		};

		// Act 2: Opponent tries to resolve the valid action
		engine.Resolve(state, opponentCtx, opAction);

		// Assert: No effect should happen
		Assert.IsNull(opponent.Board.FirstOrDefault(m => m.Name == "Choice1"),
			"Opponent should NOT be able to execute the player's choice");

		Assert.IsNotNull(state.PendingChoice,
			"PendingChoice must remain because opponent action is invalid");

		Assert.AreEqual(0, current.Board.Count, "No minion should be summoned");
	}
}
