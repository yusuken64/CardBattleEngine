using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardBattleEngine.Test;

[TestClass]
public class TargetTest
{
	[TestMethod]
	public void StealthUntargetableWithSpell()
	{

		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		var opponent = state.Players[1];

		MinionCard stealthMinionCard = new MinionCard("stealth", 1, 1, 1)
		{
			IsStealth = true,
		};
		Minion stealthMinion = new Minion(stealthMinionCard, opponent);
		opponent.Board.Add(stealthMinion);

		SpellCard fireball = new SpellCard("fireball", 0);
		fireball.SpellCastEffects.Add(new SpellCastEffect()
		{
			AffectedEntitySelector = new ContextSelector()
			{
				IncludeTarget = true,
			},
			GameActions = [new DamageAction() {
				Damage = (Value) 6
			}]
		});
		
		current.Hand.Add(fireball);
		fireball.Owner = current;

		var playCardAction = new PlayCardAction()
		{
			Card = fireball
		};

		var canTargetOpponent = playCardAction.IsValid(state, new ActionContext()
		{
			Source = current,
			Target = opponent
		}, out _);

		Assert.IsTrue(canTargetOpponent);
		var canTargetStealthMinion = playCardAction.IsValid(state, new ActionContext()
		{
			Source = current,
			Target = stealthMinion
		}, out _);

		Assert.IsFalse(canTargetStealthMinion);
	}

	[TestMethod]
	public void StealthUntargetableWithAttack()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		current.Attack = 1;

		var opponent = state.Players[1];

		MinionCard stealthMinionCard = new MinionCard("stealth", 1, 1, 1)
		{
			IsStealth = true,
		};
		Minion stealthMinion = new Minion(stealthMinionCard, opponent);
		opponent.Board.Add(stealthMinion);

		var attackAction = new AttackAction();

		var canTargetOpponent = attackAction.IsValid(state, new ActionContext()
		{
			Source = current,
			Target = opponent
		}, out _);

		Assert.IsTrue(canTargetOpponent);
		var canTargetStealthMinion = attackAction.IsValid(state, new ActionContext()
		{
			Source = current,
			Target = stealthMinion
		}, out _);

		Assert.IsFalse(canTargetStealthMinion);
	}

	[TestMethod]
	public void ElusiveUntargetableWithSpell()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		var opponent = state.Players[1];

		MinionCard elusiveMinionCard = new MinionCard("elusive", 1, 1, 1)
		{
			Elusive = true,
		};
		Minion elusiveMinion = new Minion(elusiveMinionCard, opponent);
		opponent.Board.Add(elusiveMinion);

		SpellCard fireball = new SpellCard("fireball", 0);
		fireball.SpellCastEffects.Add(new SpellCastEffect()
		{
			AffectedEntitySelector = new ContextSelector()
			{
				IncludeTarget = true,
			},
			GameActions = [new DamageAction() {
				Damage = (Value) 6
			}]
		});

		current.Hand.Add(fireball);
		fireball.Owner = current;

		var playCardAction = new PlayCardAction()
		{
			Card = fireball
		};

		var canTargetOpponent = playCardAction.IsValid(state, new ActionContext()
		{
			Source = current,
			Target = opponent
		}, out _);

		Assert.IsTrue(canTargetOpponent);
		var canTargetElusiveMinion = playCardAction.IsValid(state, new ActionContext()
		{
			Source = current,
			Target = elusiveMinion
		}, out _);

		Assert.IsFalse(canTargetElusiveMinion);
	}

	[TestMethod]
	public void BGH_TargetTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		var opponent = state.Players[1];
		current.Mana = 10;

		MinionCard BGHCard = new MinionCard("BGH", 4, 4, 2);
		BGHCard.ValidTargetSelector = new CombinedTargetSelector()
		{
			Left = new EntityTypeSelector()
			{
				EntityTypes = EntityType.Minion,
				TeamRelationship = TeamRelationship.Enemy,
			},
			Operation = CombinationOperation.And,
			Right = new StatSelector()
			{
				Stat = Stat.Attack,
				Comparison = Comparison.GreaterThanOrEqual,
				Value = 7
			}
		};

		opponent.Board.Add(new Minion(new MinionCard("Test1", 1, 1, 1), opponent));
		opponent.Board.Add(new Minion(new MinionCard("Test5", 5, 5, 5), opponent));
		opponent.Board.Add(new Minion(new MinionCard("Test7", 7, 7, 7), opponent));
		opponent.Board.Add(new Minion(new MinionCard("Test8", 8, 8, 8), opponent));

		var validTargets = BGHCard.ValidTargetSelector.Select(state, current, BGHCard);
		Assert.AreEqual(2, validTargets.Count());
	}
}
