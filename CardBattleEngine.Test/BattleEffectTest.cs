namespace CardBattleEngine.Test;

[TestClass]
public class BattleEffectTest
{
	[TestMethod]
	public void FreezeTest_AllCases()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine(new XorShiftRNG(1));
		var player1 = state.Players[0];
		var player2 = state.Players[1];

		engine.StartGame(state);

		// Add minions to both boards
		var minionCard1 = new MinionCard("TestMinion1", 1, 2, 2) { Owner = player1 };
		var minionCard2 = new MinionCard("TestMinion2", 1, 2, 2) { Owner = player2 };

		var minion1 = new Minion(minionCard1, player1);
		var minion2 = new Minion(minionCard2, player2);

		player1.Board.Add(minion1);
		player2.Board.Add(minion2);

		var freezeMinionAction = new FreezeAction();
		engine.Resolve(state, new ActionContext { Target = minion2, SourcePlayer = player1 }, freezeMinionAction);

		//enemy minion is frozen and hasn't missed attack yet
		Assert.IsTrue(minion2.IsFrozen, "Enemy minion should be frozen");
		Assert.IsFalse(minion2.MissedAttackFromFrozen, "Minion has not yet missed an attack");

		//Freeze enemy hero
		var freezeHeroAction = new FreezeAction();
		engine.Resolve(state, new ActionContext { Target = player2, SourcePlayer = player1 }, freezeHeroAction);

		Assert.IsTrue(player2.IsFrozen, "Enemy hero should be frozen");

		// Act 2: Start frozen player's turn -> mark that freeze caused missed attack
		engine.Resolve(state, new ActionContext { SourcePlayer = player1 }, new EndTurnAction());

		// Assert 2: minion is still frozen but now counted as missed attack
		Assert.IsTrue(minion2.IsFrozen, "Enemy minion should still be frozen at start of turn");
		Assert.IsTrue(minion2.MissedAttackFromFrozen, "Minion missed attack due to freeze");

		// Act 3: Attempt attack with frozen minion
		var attackAction = new AttackAction();
		var attackContext = new ActionContext { Source = minion2, Target = minion1, SourcePlayer = player2 };
		Assert.IsFalse(attackAction.IsValid(state, attackContext), "Frozen minion cannot attack");

		// Act 5: End frozen player's turn -> freeze should wear off
		engine.Resolve(state, new ActionContext { SourcePlayer = player2 }, new EndTurnAction());

		// Assert 4: freeze removed from minion and hero
		Assert.IsFalse(minion2.IsFrozen, "Enemy minion freeze should wear off at end of turn");
		Assert.IsFalse(minion2.MissedAttackFromFrozen, "MissedAttackFromFrozen flag should be reset");
		Assert.IsFalse(player2.IsFrozen, "Enemy hero freeze should wear off at end of turn");
	}
}
