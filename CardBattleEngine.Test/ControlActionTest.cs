using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CardBattleEngine.Test;

[TestClass]
public class ControlActionTest
{
	[TestMethod]
	public void RepeatTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player1 = state.Players[0];
		var player2 = state.Players[1];
		int count = 0;
		player1.Mana = 1;

		SpellCard spellCard = new SpellCard("Repeat Spell", 1);
		spellCard.SpellCastEffects.Add(new SpellCastEffect()
		{
			GameActions = [new RepeatAction()
			{
				Count = (Value)3,
				ChildActions = [new DebugLambaAction() {
					IsValidFunc = (state, context) => true,
					ResolveFunc = (state, context) =>
					{
						count++;
						return [];
					}
				}]
			}]
		});

		spellCard.Owner = player1;
		player1.Hand.Add(spellCard);

		engine.Resolve(
			state,
			new ActionContext()
			{
				SourcePlayer =  player1,
				Source = player1
			},
			new PlayCardAction()
			{
				Card = spellCard
			});

		Assert.AreEqual(3, count);
	}
	[TestMethod]
	public void SequentialTest_BladeFlurryNoWeapon()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player1 = state.Players[0];
		var player2 = state.Players[1];
		int count = 0;
		player1.Mana = 1;

		SpellCard spellCard = new SpellCard("Sequence Spell", 1);
		spellCard.SpellCastEffects.Add(new SpellCastEffect()
		{
			GameActions = [new SequentialAction()
			{
				Effects = [
					new SequentialEffect()
					{
						GameActions = [new AssignVariableAction()
						{
							VariableName = "Test",
							Value = new WeaponValue()
							{
								Side = TeamRelationship.Friendly
							}
						}],
						AffectedEntitySelector = new ContextSelector()
						{
							IncludeSourcePlayer = true,
						}
					},
					new SequentialEffect()
					{
						GameActions = [ new DamageAction()
						{
							Damage = new VariableValue()
							{
								VariableName = "Test"
							}
						}],
						AffectedEntitySelector = new TargetOperationSelector()
						{
							Operations = [new SelectBoardEntitiesOperation() {
								Group = TargetGroup.All,
								Side = TeamRelationship.Any
							}]
						}
					},
					new SequentialEffect()
					{
						GameActions = [new DestroyWeaponAction()],
						AffectedEntitySelector = new ContextSelector()
						{
							IncludeSourcePlayer = true,
						}
					}
				]
			}],
			AffectedEntitySelector = new ContextSelector()
			{
				IncludeSourcePlayer = true,
			}
		});

		spellCard.Owner = player1;
		player1.Hand.Add(spellCard);

		engine.Resolve(
			state,
			new ActionContext()
			{
				SourcePlayer = player1,
				Source = player1
			},
			new PlayCardAction()
			{
				Card = spellCard
			});
	}

	[TestMethod]
	public void SequentialTest_BladeFlurryWeapon()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();
		var player1 = state.Players[0];
		var player2 = state.Players[1];
		int count = 0;
		player1.Mana = 1;

		player1.EquipWeapon(new Weapon("test", 2, 1));
		MinionCard card1 = new MinionCard("1/1", 1, 1, 1);
		MinionCard card2 = new MinionCard("2/2", 2, 2, 2);
		MinionCard card3 = new MinionCard("3/3", 3, 3, 3);
		player2.Board.Add(new Minion(card1, player2));
		player2.Board.Add(new Minion(card2, player2));
		player2.Board.Add(new Minion(card3, player2));

		SpellCard spellCard = new SpellCard("Sequence Spell", 1);
		spellCard.SpellCastEffects.Add(new SpellCastEffect()
		{
			GameActions = [new SequentialAction()
			{
				Effects = [
					new SequentialEffect()
					{
						GameActions = [new AssignVariableAction()
						{
							VariableName = "Test",
							Value = new WeaponValue()
							{
								Side = TeamRelationship.Friendly
							}
						}],
						AffectedEntitySelector = new ContextSelector()
						{
							IncludeSourcePlayer = true,
						}
					},
					new SequentialEffect()
					{
						GameActions = [	new DamageAction()
						{
							Damage = new VariableValue()
							{
								VariableName = "Test"
							}
						}],
						AffectedEntitySelector = new TargetOperationSelector()
						{
							Operations = [new SelectBoardEntitiesOperation() {
								Group = TargetGroup.All,
								Side = TeamRelationship.Any
							}]
						}
					},
				]
			}],
			AffectedEntitySelector = new ContextSelector()
			{
				IncludeSourcePlayer = true,
			}
		});
		spellCard.SpellCastEffects.Add(new SpellCastEffect()
		{
			GameActions = [
				new DeferredResolveAction()
				{
					Action = new DestroyWeaponAction(),
					AffectedEntitySelector = new ContextSelector()
					{
						IncludeSourcePlayer = true
					}
				}],
		});

		spellCard.Owner = player1;
		player1.Hand.Add(spellCard);

		Assert.IsNotNull(player1.EquippedWeapon);
		Assert.AreEqual(3, player2.Board.Count);
		Assert.AreEqual(30, player2.Health);

		engine.Resolve(
			state,
			new ActionContext()
			{
				SourcePlayer = player1,
				Source = player1
			},
			new PlayCardAction()
			{
				Card = spellCard
			});

		Assert.AreEqual(1, player2.Board.Count);
		Assert.IsNull(player1.EquippedWeapon);
		Assert.AreEqual(28, player2.Health);
	}
}
