namespace CardBattleEngine.Test;

[TestClass]
public class DiscoverTests
{
	[TestMethod]
	public void DiscoverFromDeck_ReturnsThreeRandomCards()
	{
		var engine = new GameEngine();
		var state = GameFactory.CreateTestGame();
		var player = state.Players[0];

		var choice = new DiscoverChoice()
		{
			SourcePlayer = player,
			SourceProvider = new DeckProvider(),
			OptionCount = 3,
			ActionFactory = new DiscoverActionFactory()
			{
				DiscoverAction = DiscoverAction.DrawTargetFromDeck
			}
		};

		var actions = choice.GetActions(state).ToList();

		Assert.AreEqual(3, actions.Count);
		CollectionAssert.AllItemsAreUnique(actions);

		engine.Resolve(state, actions[0].Item2, actions[0].Item1);
		var drawCardAction = actions[0].Item1 as DrawTargetCardFromDeckAction;
		Assert.IsTrue(player.Hand.Contains(actions[0].Item2.Target));
	}

	[TestMethod]
	public void DiscoverCostTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player = state.Players[0];

		var discoverSpellCard = new SpellCard("DiscoverSpell", 1);
		discoverSpellCard.SpellCastEffects.Add(new SpellCastEffect()
		{
			GameActions = [new RequestChoiceAction()
			{
				PendingChoice = new DiscoverChoice()
				{
					ActionFactory = new DiscoverActionFactory()
					{
						DiscoverAction = DiscoverAction.Gain,
					},
					Filters = [new CostFilter() {
						Cost = 2
					}],
					OptionCount = 3,
					SourcePlayer = state.Players[0],
					SourceProvider = new CardDBProvider()
				}
			}],
			AffectedEntitySelector = new ContextSelector()
			{
				IncludeSourcePlayer = true
			}
		});

		// Cast the spell (should push the RequestChoiceAction)
		engine.Resolve(
			state,
			new ActionContext()
			{
				SourcePlayer = player,
				Source = discoverSpellCard,
				SourceCard = discoverSpellCard
			},
			new CastSpellAction());

		// Pull the pending discover choice
		var choices = state.PendingChoice.Options.ToList();

		// Should produce exactly 3 options
		Assert.AreEqual(3, choices.Count());

		// All items should be unique
		CollectionAssert.AllItemsAreUnique(choices);

		// Resolve choosing the first option
		var (firstAction, firstCtx) = choices[0];

		Assert.AreSame(
			state.PendingChoice.Options.ToList()[0].Item1,
			firstAction,
			"Action instance mismatch!");

		engine.Resolve(state, firstCtx, firstAction);

		// The player should now have gained that card
		Assert.IsTrue(player.Hand.Contains((firstAction as GainCardAction).Card),
			"Player did not gain the discovered card.");
	}
}
