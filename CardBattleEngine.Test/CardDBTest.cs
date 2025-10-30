using System.ComponentModel.DataAnnotations;

namespace CardBattleEngine.Test;

[TestClass]
[DoNotParallelize]
public class CardDBTest
{
	//public string DBPath = "C:\\Users\\yusuk\\source\\repos\\CardBattleEngine\\Data\\";
	public static string DBPath = Path.Combine(AppContext.BaseDirectory, "Data");

	[TestMethod]
	public void LoadDBTest()
	{
		CardDatabase cardDatabase = new(DBPath);

		Player owner = new Player("Test");
		MinionCard minion = cardDatabase.GetMinionCard("TestMinion", owner);

		Assert.IsNotNull(minion);
	}

	[TestMethod]
	public void CreateMinionDefinitionTest()
	{
		MinionCard card = new MinionCard("SaveTest", 2, 2, 2);
		CardDatabase.CreateFileFromMinionCard(card, DBPath, "SaveTestMinion");
	}

	[TestMethod]
	public void CreateMinionDefinitionTest2()
	{
		var testGame = GameFactory.CreateTestGame();
		var current = testGame.Players[0];
		var opponent = testGame.Players[1];

		var card = new MinionCard("BattlecryMinion", cost: 1, attack: 1, health: 1);
		card.Owner = current;
		card.TriggeredEffects.Add(new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.Battlecry,
			EffectTiming = EffectTiming.Post,
			TargetType = TargetingType.AnyEnemy,
			GameActions = new List<IGameAction>()
			{
				new DamageAction()
				{
					Damage = 1,
				}
			}
		});

		CardDatabase.CreateFileFromMinionCard(card, ".\\Data\\", "BattleCryMinion");

		CardDatabase testDB = new CardDatabase(".\\Data\\");
		var loadedMinion = testDB.GetMinionCard("BattleCryMinion", current);

		Assert.AreEqual(1, loadedMinion.Attack);
		Assert.AreEqual(1, loadedMinion.Health);
		Assert.AreEqual(1, loadedMinion.ManaCost);
		Assert.AreEqual(1, loadedMinion.TriggeredEffects.Count());
		Assert.AreEqual(EffectTrigger.Battlecry, loadedMinion.TriggeredEffects[0].EffectTrigger);
		Assert.AreEqual(EffectTiming.Post, loadedMinion.TriggeredEffects[0].EffectTiming);
		Assert.AreEqual(TargetingType.AnyEnemy, loadedMinion.TriggeredEffects[0].TargetType);
		Assert.AreEqual(TargetingType.AnyEnemy, loadedMinion.TriggeredEffects[0].TargetType);
		Assert.AreEqual(1, loadedMinion.TriggeredEffects[0].GameActions.Count());
		Assert.IsInstanceOfType(loadedMinion.TriggeredEffects[0].GameActions[0], typeof(DamageAction));
		Assert.AreEqual(1, ((DamageAction)loadedMinion.TriggeredEffects[0].GameActions[0]).Damage);
	}

	[TestMethod]
	public void LoadDBTriggereEffectTest()
	{
		var db = new CardDatabase(DBPath);
		Player owner = new Player("Test");
		var card = db.GetMinionCard("BattleCryMinion", owner);

		Assert.AreEqual(1, card.TriggeredEffects.Count, "Expected 1 triggered effect.");
		var effect = card.TriggeredEffects[0];
		Assert.AreEqual(EffectTrigger.Battlecry, effect.EffectTrigger);
		Assert.AreEqual(EffectTiming.Post, effect.EffectTiming);
		Assert.AreEqual(TargetingType.AnyEnemy, effect.TargetType);
		Assert.AreEqual(1, effect.GameActions.Count);
		Assert.IsInstanceOfType(effect.GameActions[0], typeof(DamageAction));
	}

	[TestMethod]
	public void LoadDBTriggereEffectWithConditionTest()
	{
		// Arrange
		var db = new CardDatabase(DBPath);
		Player owner = new Player("Test");

		// Act
		var card = db.GetMinionCard("Murloc", owner);

		// Assert: card has exactly one triggered effect
		Assert.AreEqual(1, card.TriggeredEffects.Count, "Expected 1 triggered effect.");
		var effect = card.TriggeredEffects[0];

		// Assert: Effect timing and trigger type are correct
		Assert.AreEqual(EffectTiming.Pre, effect.EffectTiming, "Unexpected effect timing.");
		Assert.AreEqual(EffectTrigger.SummonMinion, effect.EffectTrigger, "Unexpected effect trigger.");

		// Assert: Trigger condition is not null and is correct type
		Assert.IsNotNull(effect.Condition, "Expected a trigger condition.");
		Assert.AreEqual("SummonedMinionTribeCondition", effect.Condition.GetType().Name, "Unexpected condition type.");

		// Assert: Condition parameters match expected (Murloc tribe)
		var tribeParam = effect.Condition.EmitParams()["MinionTribe"].ToString();
		Assert.AreEqual("Murloc", tribeParam, "Trigger condition should match Murloc tribe.");

		// Assert: GameActions are loaded correctly
		Assert.IsNotNull(effect.GameActions, "Expected at least one game action.");
		Assert.AreEqual(1, effect.GameActions.Count, "Expected exactly one game action.");

		var action = effect.GameActions[0];
		Assert.AreEqual("AddStatModifierAction", action.GetType().Name, "Unexpected game action type.");

		// Optional: verify default modifier params
		var damageParam = action.EmitParams();
		Assert.AreEqual(1, damageParam["AttackChange"], "Expected +1 attack from triggered effect.");
		Assert.AreEqual(0, damageParam["HealthChange"], "Expected 0 health change from triggered effect.");
	}


	[TestMethod]
	public void CreateSpellDefinitionTest()
	{
		SpellCard card = new SpellCard("TestSpell_DrawCards", 1);
		card.TargetingType = TargetingType.None;
		card.SpellCastEffects.Add(new SpellCastEffect());

		card.SpellCastEffects[0] = new SpellCastEffect()
		{
			GameActions = new()
			{
				new DrawCardFromDeckAction(),
				new DrawCardFromDeckAction(),
				new DrawCardFromDeckAction(),
			}
		};
		var json = CardDatabase.CreateFileFromSpellCard(card, DBPath, "SaveTestSpell");
		Console.WriteLine(json);
	}

	[TestMethod]
	public void CreateSpellDefinitionTest2()
	{
		SpellCard card = new SpellCard("TestSpell_AOEDamage", 1);
		card.TargetingType = TargetingType.None;

		card.SpellCastEffects.Add(new SpellCastEffect()
		{
			AffectedEntitySelector = new TargetOperationSelector()
			{
				//Side = TargetSide.Enemy,
				//Group = TargetGroup.Minions,
			},
			GameActions = new()
			{
				new DamageAction()
				{
					Damage = 5,
				}
			}
		});
		var json = CardDatabase.CreateFileFromSpellCard(card, DBPath, "SaveTestSpell");
		Console.WriteLine(json);
	}

	[TestMethod]
	public void CreateTargetedSpellDefinitionTest()
	{
		SpellCard card = new SpellCard("TestSpell_DealDamage", 1);
		card.SpellCastEffects.Add(new SpellCastEffect());
		card.TargetingType = TargetingType.Any;

		card.SpellCastEffects[0] = new SpellCastEffect()
		{
			GameActions = new()
			{
				new DamageAction() { Damage = 3 },
				new FreezeAction(),
			}
		};
		var json = CardDatabase.CreateFileFromSpellCard(card, DBPath, "SaveTestSpell");
		Console.WriteLine(json);
	}
}
