namespace CardBattleEngine.Test
{
	[TestClass]
	public sealed class CloneTests
	{
		[TestMethod]
		public void CloneTest()
		{
			var gameState = GameFactory.CreateTestGame();
			var engine = new GameEngine(new XorShiftRNG(1));

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

				orignalActions[0].Item2.TargetSelector = (gs, player, targetType) =>
				{
					return gs.GetValidTargets(player, targetType)[0];
				};
				
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
	}
}
