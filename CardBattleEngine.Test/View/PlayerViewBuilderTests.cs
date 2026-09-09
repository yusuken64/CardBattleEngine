using CardBattleEngine.View;

namespace CardBattleEngine.Test;

[TestClass]
public class PlayerViewBuilderTests
{
	[TestMethod]
	public void OpponentView_NeverContainsHandOrSecretContents()
	{
		var state = GameFactory.CreateTestGame();
		var player1 = state.Players[0];
		var player2 = state.Players[1];

		var engine = new GameEngine();
		engine.StartGame(state);

		var viewOfPlayer1 = PlayerViewBuilder.Build(state, player1);
		var viewOfPlayer2 = PlayerViewBuilder.Build(state, player2);

		Assert.IsNull(viewOfPlayer1.Opponent.Hand, "Opponent hand contents must never be sent.");
		Assert.IsNull(viewOfPlayer1.Opponent.Secrets, "Opponent secret contents must never be sent.");
		Assert.AreEqual(player2.Hand.Count, viewOfPlayer1.Opponent.HandCount);

		Assert.IsNull(viewOfPlayer2.Opponent.Hand, "Opponent hand contents must never be sent.");
		Assert.IsNull(viewOfPlayer2.Opponent.Secrets, "Opponent secret contents must never be sent.");
		Assert.AreEqual(player1.Hand.Count, viewOfPlayer2.Opponent.HandCount);

		Assert.IsNotNull(viewOfPlayer1.Self.Hand, "A player must see their own hand.");
		Assert.AreEqual(player1.Hand.Count, viewOfPlayer1.Self.Hand.Count);
	}

	[TestMethod]
	public void DeckContents_AreAlwaysCountOnly_ForBothSelfAndOpponent()
	{
		var state = GameFactory.CreateTestGame();
		var player1 = state.Players[0];

		var view = PlayerViewBuilder.Build(state, player1);

		Assert.AreEqual(player1.Deck.Count, view.Self.DeckCount);
		Assert.AreEqual(state.OpponentOf(player1).Deck.Count, view.Opponent.DeckCount);
	}

	[TestMethod]
	public void PendingDiscoverChoice_IsHiddenFromNonSourcePlayer()
	{
		var state = GameFactory.CreateTestGame();
		var player = state.Players[0];
		var opponent = state.Players[1];

		state.PendingChoice = new DiscoverChoice
		{
			SourcePlayer = player,
			SourceProvider = new DeckProvider(),
			OptionCount = 3,
			ActionFactory = new DiscoverActionFactory
			{
				DiscoverAction = DiscoverAction.Gain,
			},
		};

		var viewOfSource = PlayerViewBuilder.Build(state, player);
		var viewOfOpponent = PlayerViewBuilder.Build(state, opponent);

		Assert.IsNotNull(viewOfSource.PendingChoice, "The choosing player must see their own options.");
		Assert.AreEqual(3, viewOfSource.PendingChoice.Options.Count);
		Assert.IsFalse(viewOfSource.OpponentIsChoosing);

		Assert.IsNull(viewOfOpponent.PendingChoice, "The non-source player must never see Discover candidates.");
		Assert.IsTrue(viewOfOpponent.OpponentIsChoosing);
		Assert.AreEqual(0, viewOfOpponent.LegalActions.Count);
	}

	[TestMethod]
	public void LegalActions_OnlyPopulatedForCurrentPlayer()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		engine.StartGame(state);

		// Mulligan is a PendingChoice for player 1 immediately after StartGame.
		var mulligan = state.PendingChoice.GetActions(state).First();
		engine.Resolve(state, mulligan.Item2, mulligan.Item1);

		// Player 2 must also mulligan before turn 1 begins.
		var opponentMulligan = state.PendingChoice.GetActions(state).First();
		engine.Resolve(state, opponentMulligan.Item2, opponentMulligan.Item1);

		var current = state.CurrentPlayer;
		var other = state.OpponentOf(current);

		var viewOfCurrent = PlayerViewBuilder.Build(state, current);
		var viewOfOther = PlayerViewBuilder.Build(state, other);

		Assert.IsTrue(viewOfCurrent.LegalActions.Count > 0, "The current player must have legal actions.");
		Assert.AreEqual(0, viewOfOther.LegalActions.Count, "A non-current player must have no legal actions.");
	}

	[TestMethod]
	public void SecretIdentity_IsHiddenFromOpponent_UntilItResolves()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player1 = state.Players[0];
		var player2 = state.Players[1];

		player1.Mana = 1;
		player2.Mana = 2;

		var secret = new Secret
		{
			SecretTrigger = new TriggeredEffect
			{
				EffectTrigger = EffectTrigger.SpellCast,
				EffectTiming = EffectTiming.Pre,
				GameActions = [new CancelEffectAction()],
				Condition = new SourceOwnerCondition { TeamRelationship = TeamRelationship.Enemy },
				AffectedEntitySelector = new ContextSelector { IncludeSourcePlayer = true },
			},
		};

		var secretCard = new SpellCard("CounterSpell", 1);
		secretCard.SpellCastEffects.Add(new SpellCastEffect
		{
			GameActions = [new SecretAction { Secret = secret }],
		});
		secretCard.Owner = player1;
		player1.Hand.Add(secretCard);

		int historyBeforeCast = state.History.Count;
		engine.Resolve(state, new ActionContext
		{
			SourcePlayer = player1,
			SourceCard = secretCard,
			Target = player1,
		}, new PlayCardAction { Card = secretCard });

		var castEntries = state.History.Skip(historyBeforeCast).ToList();
		var viewForOpponent = PlayerViewBuilder.Build(state, player2, castEntries);
		var viewForOwner = PlayerViewBuilder.Build(state, player1, castEntries);

		Assert.IsTrue(
			viewForOpponent.NewHistory.All(h => h.ActionType != "PlayCardAction" && h.SourceId == null || h.ActionType == "SecretPlayed"),
			"The opponent must never see the real card/action behind an unresolved secret.");
		Assert.IsTrue(
			viewForOpponent.NewHistory.Any(h => h.ActionType == "SecretPlayed"),
			"The opponent should still see that a secret was played.");
		Assert.IsTrue(
			viewForOwner.NewHistory.Any(h => h.SourceId == secretCard.Id),
			"The owner must still see their own played card identity.");

		// Trigger the secret: player2 casts a spell, which should resolve and remove the secret.
		var fireball = new SpellCard("Fireball", 0);
		fireball.Owner = player2;
		player2.Hand.Add(fireball);

		int historyBeforeTrigger = state.History.Count;
		engine.Resolve(state, new ActionContext
		{
			SourcePlayer = player2,
			SourceCard = fireball,
			Target = player1,
		}, new PlayCardAction { Card = fireball });

		Assert.AreEqual(0, player1.Secrets.Count, "Secret should have resolved and been removed.");

		// Re-view the ORIGINAL cast entries now that the secret has resolved: they should no longer be redacted.
		var viewForOpponentAfterResolve = PlayerViewBuilder.Build(state, player2, castEntries);
		Assert.IsTrue(
			viewForOpponentAfterResolve.NewHistory.Any(h => h.SourceId == secretCard.Id),
			"Once a secret has resolved, the opponent should be able to see what it actually was.");
	}
}
