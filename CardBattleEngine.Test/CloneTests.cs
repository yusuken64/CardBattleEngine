using Microsoft.VisualStudio.TestPlatform.Common.Utilities;

namespace CardBattleEngine.Test
{
	[TestClass]
	public sealed class CloneTests
	{
		[TestMethod]
		public void CloneTest()
		{
			var gameState = GameFactory.CreateTestGame();
			var engine = new GameEngine();

			engine.StartGame(gameState);

			while (!gameState.IsGameOver())
			{
				List<(IGameAction, ActionContext)> orignalActions = gameState.GetValidActions(gameState.CurrentPlayer);

				var clonedGameState = gameState.Clone();
				var clonedActions = clonedGameState.GetValidActions(clonedGameState.CurrentPlayer);

				Assert.AreEqual(orignalActions.Count, clonedActions.Count);

				for (int i = 0; i < orignalActions.Count; i++)
				{
					Assert.AreEqual(orignalActions[i].ToString(), clonedActions[i].ToString());
				}

				Assert.IsTrue(orignalActions[0].Item1.IsValid(gameState, orignalActions[0].Item2, out _));

				engine.Resolve(gameState, orignalActions[0].Item2, orignalActions[0].Item1);

				GameEngine.PrintState(gameState, null);
			}
		}

		[TestMethod]
		public void CloneCard()
		{
			var player = new Player("TestPlayer");
			var card = new MinionCard("TestMinion", 1, 2, 3)
			{
				Owner = player
			};

			var clonedCard = card.Clone();

			Assert.AreEqual(card.Owner, clonedCard.Owner);
		}

		[TestMethod]
		public void CloneHand()
		{
			var player = new Player("TestPlayer");
			player.Hand.Add(new MinionCard("Minion1", 1, 1, 1) { Owner = player });
			player.Hand.Add(new MinionCard("Minion2", 2, 2, 2) { Owner = player });
			player.Hand.Add(new MinionCard("Minion3", 3, 3, 3) { Owner = player });
			player.Hand.Add(new MinionCard("Minion4", 4, 4, 4) { Owner = player });

			var clonedPlayer = player.Clone();

			Assert.AreEqual(player.ToString(), clonedPlayer.ToString());

			for (int i = 0; i < player.Hand.Count; i++)
			{
				Card? card = player.Hand[i];
				Card? clonedCard = clonedPlayer.Hand[i];

				Assert.AreEqual(card.Owner, player);
				Assert.AreEqual(clonedCard.Owner, clonedPlayer);
			}
		}

		[TestMethod]
		public void CloneActionTest()
		{
			var gameState = GameFactory.CreateTestGame();
			var engine = new GameEngine();
			var current = gameState.Players[0];

			MinionCard testCard = new MinionCard("Test", 1, 1, 1);
			testCard.Owner = current;
			current.Hand.Add(testCard);
			{
				PlayCardAction playCardAction = new PlayCardAction()
				{
					Card = testCard
				};

				ActionContext context = new ActionContext()
				{
					Source = current,
				};
				var isValid = playCardAction.IsValid(gameState, context, out string reason);
				Assert.IsTrue(isValid, reason);
			}
			{
				var clonedState = gameState.Clone();

				Card clonedTestCard = (Card)clonedState.GetEntityById(testCard.Id);
				Player clonedPlayer = clonedState.Players[0];
				Assert.IsTrue(clonedPlayer.Hand.Contains(clonedTestCard));
				Assert.AreEqual(clonedTestCard.Owner, clonedPlayer);

				PlayCardAction playCardAction = new PlayCardAction()
				{
					Card = clonedTestCard
				};
				ActionContext context = new ActionContext()
				{
					SourcePlayer = clonedPlayer,
					Source = clonedPlayer,
				};
				var isValid = playCardAction.IsValid(clonedState, context, out string reason);
				Assert.IsTrue(isValid, reason);

				engine.Resolve(clonedState, context, playCardAction);

				Assert.IsTrue(!clonedPlayer.Hand.Contains(clonedTestCard));
			}
		}

		[TestMethod]
		public void CloneActionTest2()
		{
			var gameState = GameFactory.CreateTestGame();
			var engine = new GameEngine();
			var current = gameState.Players[0];

			MinionCard testCard = new MinionCard("Test", 1, 1, 1);
			testCard.Owner = current;
			current.Hand.Add(testCard);

			var actions = gameState.GetValidActions(current);

			Assert.IsTrue(actions.Any(x => x.Item1 is PlayCardAction));
			var originalPlaycardAction = actions.First(x => x.Item1 is PlayCardAction);

			{
				var clonedState = gameState.Clone();

				Card clonedTestCard = (Card)clonedState.GetEntityById(testCard.Id);
				Player clonedPlayer = clonedState.Players[0];
				Assert.IsTrue(clonedPlayer.Hand.Contains(clonedTestCard));
				Assert.AreEqual(clonedTestCard.Owner, clonedPlayer);

				PlayCardAction playCardAction = new PlayCardAction()
				{
					Card = clonedTestCard
				};
				ActionContext context = new ActionContext()
				{
					SourcePlayer = clonedPlayer,
					Source = clonedPlayer,
				};
				var isValid = playCardAction.IsValid(clonedState, context, out string reason);
				Assert.IsTrue(isValid, reason);

				engine.Resolve(clonedState, context, playCardAction);

				Assert.IsTrue(!clonedPlayer.Hand.Contains(clonedTestCard));
			}

		}

		[TestMethod]
		public void CloneVariable()
		{
			ActionContext context = new ActionContext();

			context.SetVar();
		}
	}
}
