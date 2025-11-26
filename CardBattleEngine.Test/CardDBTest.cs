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
					Damage = (Value)1,
				}
			},
			
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
		Assert.AreEqual(1, ((DamageAction)loadedMinion.TriggeredEffects[0].GameActions[0]).Damage.GetValue(testGame, null));
	}

	[TestMethod]
	public void LoadMurlocTribeTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);

		CardDatabase cardDatabase = new(CardDBTest.DBPath);

		// Get two different Murlocs to summon
		var firstMurlocCard = cardDatabase.GetMinionCard("Murloc", current);
		var secondMurlocCard = cardDatabase.GetMinionCard("Murloc", current);

		current.Mana = 3; // enough to play both

		current.Hand.Add(firstMurlocCard);
		current.Hand.Add(secondMurlocCard);

		// Act 1: Play the first Murloc
		engine.Resolve(state, new ActionContext
		{
			SourcePlayer = current,
			SourceCard = firstMurlocCard
		}, new PlayCardAction { Card = firstMurlocCard });

		var firstMurloc = current.Board[0];
		Assert.IsTrue(firstMurloc.Tribes.Contains(MinionTribe.Murloc));
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
		Assert.AreEqual(EffectTiming.Post, effect.EffectTiming, "Unexpected effect timing.");
		Assert.AreEqual(EffectTrigger.SummonMinion, effect.EffectTrigger, "Unexpected effect trigger.");

		// Assert: Trigger condition is not null and is correct type
		Assert.IsNotNull(effect.Condition, "Expected a trigger condition.");
		Assert.AreEqual("SummonedMinionCondition", effect.Condition.GetType().Name, "Unexpected condition type.");

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
		var json = CardDatabase.CreateJsonFromSpellCard(card, "SaveTestSpell");
		Console.WriteLine(json);
	}

	[TestMethod]
	public void CreateSpellDefinitionTest2()
	{
		// Arrange
		SpellCard card = new SpellCard("TestSpell_AOEDamage", 1);
		card.TargetingType = TargetingType.None;

		card.SpellCastEffects.Add(new SpellCastEffect()
		{
			AffectedEntitySelector = new TargetOperationSelector()
			{
				Operations = new List<ITargetOperation>()
				{
					new SelectBoardEntitiesOperation()
					{
						Group = TargetGroup.Minions,
						Side = TeamRelationship.Enemy
					},
				}
			},
			GameActions = new()
		{
			new DamageAction()
			{
				Damage = (Value) 5,
			}
		}
		});

		// Act
		var json = CardDatabase.CreateJsonFromSpellCard(card, "SaveTestSpell");
		Console.WriteLine(json);

		var loadedCard = CardDatabase.LoadCardFromJson(json);

		// Assert
		Assert.IsNotNull(loadedCard, "Loaded card should not be null");
		Assert.IsInstanceOfType(loadedCard, typeof(SpellCardDefinition), "Loaded card should be a SpellCardDefinition");

		var spellDef = (SpellCardDefinition)loadedCard;

		Assert.AreEqual("SaveTestSpell", spellDef.Id, "Card Id should match");
		Assert.AreEqual(card.Name, spellDef.Name, "Card name should match");
		Assert.AreEqual(card.ManaCost, spellDef.Cost, "Mana cost should match");
		Assert.AreEqual(TargetingType.None, spellDef.TargetingType, "TargetingType should match");
		Assert.AreEqual(CardType.Spell, spellDef.Type, "Type should be Spell");

		// Check that SpellCastEffectDefinitions was serialized correctly
		Assert.AreEqual(1, spellDef.SpellCastEffectDefinitions.Count, "Should have one SpellCastEffectDefinition");
		var effectDef = spellDef.SpellCastEffectDefinitions[0];

		// Verify the selector definition
		Assert.IsNotNull(effectDef.AffectedEntitySelectorDefinition, "Effect should have a selector definition");
		var selectorDef = effectDef.AffectedEntitySelectorDefinition!;
		Assert.AreEqual("TargetOperationSelector", selectorDef.EntitySelectorTypeName, "Selector type name should match");

		Assert.IsNotNull(selectorDef.Params, "Selector definition params should not be null");
		//Assert.IsTrue(selectorDef.Params.ContainsKey("Operations"), "Selector should have Operations param");

		// Check that the action definition is correct
		Assert.AreEqual(1, effectDef.ActionDefintions.Count, "Should have one GameAction");
		var damageActionDef = effectDef.ActionDefintions[0];
		Assert.AreEqual("DamageAction", damageActionDef.GameActionTypeName, "Action type should be DamageAction");

		Assert.IsTrue(damageActionDef.Params.ContainsKey("Damage"), "DamageAction should have Damage param");
		//Assert.AreEqual(5, JsonParamHelper.GetValue<int>(damageActionDef.Params, "Damage"), "Damage value should be 5");
	}


	[TestMethod]
	public void CreateTargetedSpellDefinitionTest()
	{
		// Arrange
		SpellCard card = new SpellCard("TestSpell_DealDamage", 1);
		card.TargetingType = TargetingType.Any;
		card.SpellCastEffects.Add(new SpellCastEffect()
		{
			GameActions = new()
		{
			new DamageAction() { Damage = (Value) 3 },
			new FreezeAction(),
		}
		});

		// Act
		var json = CardDatabase.CreateJsonFromSpellCard(card, "SaveTestSpell");
		var loadedCard = CardDatabase.LoadCardFromJson(json);

		// Assert
		Assert.IsNotNull(loadedCard, "Loaded card should not be null");
		Assert.IsInstanceOfType(loadedCard, typeof(SpellCardDefinition), "Loaded card should be a SpellCardDefinition");

		var spellDef = (SpellCardDefinition)loadedCard;

		Assert.AreEqual("SaveTestSpell", spellDef.Id, "Card Id should match");
		Assert.AreEqual(card.Name, spellDef.Name, "Card name should match");
		Assert.AreEqual(card.ManaCost, spellDef.Cost, "Mana cost should match");
		Assert.AreEqual(card.TargetingType, spellDef.TargetingType, "TargetingType should match");
		Assert.AreEqual(CardType.Spell, spellDef.Type, "Type should be Spell");

		// Check that SpellCastEffects were serialized and deserialized correctly
		Assert.AreEqual(1, spellDef.SpellCastEffectDefinitions.Count, "Should have one SpellCastEffectDefinition");

		var effectDef = spellDef.SpellCastEffectDefinitions[0];
		Assert.IsNotNull(effectDef.ActionDefintions, "Action definitions should not be null");
		Assert.AreEqual(2, effectDef.ActionDefintions.Count, "Should have two GameActions");

		var damageAction = effectDef.ActionDefintions[0];
		var freezeAction = effectDef.ActionDefintions[1];

		Assert.AreEqual("DamageAction", damageAction.GameActionTypeName, "First action should be DamageAction");
		Assert.AreEqual("FreezeAction", freezeAction.GameActionTypeName, "Second action should be FreezeAction");

		Assert.IsTrue(damageAction.Params.ContainsKey("Damage"), "DamageAction should have Damage param");
		//Assert.AreEqual(3, JsonParamHelper.GetValue<int>(damageAction.Params, "Damage"), "Damage value should be 3");
	}
}
