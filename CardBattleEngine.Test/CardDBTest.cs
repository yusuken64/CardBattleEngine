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
	public void LoadWeaponDBTest()
	{
		CardDatabase cardDatabase = new(DBPath);

		Player owner = new Player("Test");
		WeaponCard weapon = cardDatabase.GetWeaponCard("TestWeapon", owner);

		Assert.IsNotNull(weapon);
		Assert.AreEqual(3, weapon.Attack);
		Assert.AreEqual(2, weapon.Durability);
		Assert.AreEqual(2, weapon.ManaCost);
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
		Assert.AreEqual(1, loadedMinion.TriggeredEffects[0].GameActions.Count());
		Assert.IsInstanceOfType(loadedMinion.TriggeredEffects[0].GameActions[0], typeof(DamageAction));
		Assert.AreEqual(1, ((DamageAction)loadedMinion.TriggeredEffects[0].GameActions[0]).Damage.GetValue(testGame, null));
	}

	[TestMethod]
	public void CreateMinionDefinitionTest_MultipleActionsRoundTrip()
	{
		var testGame = GameFactory.CreateTestGame();
		var current = testGame.Players[0];

		var card = new MinionCard("MultiActionBattlecryMinion", cost: 1, attack: 1, health: 1);
		card.Owner = current;
		card.TriggeredEffects.Add(new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.Battlecry,
			EffectTiming = EffectTiming.Post,
			GameActions = new List<IGameAction>()
			{
				new DamageAction() { Damage = (Value)1 },
				new DrawCardFromDeckAction(),
			},
		});

		CardDatabase.CreateFileFromMinionCard(card, ".\\Data\\", "MultiActionBattlecryMinion");

		CardDatabase testDB = new CardDatabase(".\\Data\\");
		var loadedMinion = testDB.GetMinionCard("MultiActionBattlecryMinion", current);

		Assert.AreEqual(1, loadedMinion.TriggeredEffects.Count());
		Assert.AreEqual(2, loadedMinion.TriggeredEffects[0].GameActions.Count(), "Both actions should survive the round trip");
		Assert.IsInstanceOfType(loadedMinion.TriggeredEffects[0].GameActions[0], typeof(DamageAction));
		Assert.IsInstanceOfType(loadedMinion.TriggeredEffects[0].GameActions[1], typeof(DrawCardFromDeckAction));
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

		var firstMurloc = (Minion)current.Board[0];
		Assert.IsTrue(TribeUtils.Matches(firstMurloc.Tribes, "Murloc"));
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
		Assert.AreEqual(1, effect.GameActions.Count);
		Assert.IsInstanceOfType(effect.GameActions[0], typeof(DamageAction));
	}

	//[TestMethod]
	//public void LoadDBTriggereEffectWithConditionTest()
	//{
	//	// Arrange
	//	var db = new CardDatabase(DBPath);
	//	Player owner = new Player("Test");

	//	// Act
	//	var card = db.GetMinionCard("Murloc", owner);

	//	// Assert: card has exactly one triggered effect
	//	Assert.AreEqual(1, card.TriggeredEffects.Count, "Expected 1 triggered effect.");
	//	var effect = card.TriggeredEffects[0];

	//	// Assert: Effect timing and trigger type are correct
	//	Assert.AreEqual(EffectTiming.Post, effect.EffectTiming, "Unexpected effect timing.");
	//	Assert.AreEqual(EffectTrigger.SummonMinion, effect.EffectTrigger, "Unexpected effect trigger.");

	//	// Assert: Trigger condition is not null and is correct type
	//	Assert.IsNotNull(effect.Condition, "Expected a trigger condition.");
	//	Assert.AreEqual("SummonedMinionCondition", effect.Condition.GetType().Name, "Unexpected condition type.");

	//	// Assert: Condition parameters match expected (Murloc tribe)
	//	var tribeParam = effect.Condition.EmitParams()["MinionTribe"].ToString();
	//	Assert.AreEqual("Murloc", tribeParam, "Trigger condition should match Murloc tribe.");

	//	// Assert: GameActions are loaded correctly
	//	Assert.IsNotNull(effect.GameActions, "Expected at least one game action.");
	//	Assert.AreEqual(1, effect.GameActions.Count, "Expected exactly one game action.");

	//	var action = effect.GameActions[0];
	//	Assert.AreEqual("AddStatModifierAction", action.GetType().Name, "Unexpected game action type.");

	//	// Optional: verify default modifier params
	//	var damageParam = action.EmitParams();
	//	Assert.AreEqual(1, damageParam["AttackChange"], "Expected +1 attack from triggered effect.");
	//	Assert.AreEqual(0, damageParam["HealthChange"], "Expected 0 health change from triggered effect.");
	//}

	[TestMethod]
	public void CreateSpellDefinitionTest()
	{
		SpellCard card = new SpellCard("TestSpell_DrawCards", 1);
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
		Assert.AreEqual(CardType.Spell, spellDef.Type, "Type should be Spell");

		// Check that SpellCastEffects were serialized/deserialized natively (no intermediate Definition DTO)
		Assert.AreEqual(1, spellDef.SpellCastEffects.Count, "Should have one SpellCastEffect");
		var effect = spellDef.SpellCastEffects[0];

		// Verify the selector round-tripped as a real, concrete IAffectedEntitySelector
		Assert.IsInstanceOfType(effect.AffectedEntitySelector, typeof(TargetOperationSelector), "Selector type should match");

		// Check that the action round-tripped as a real, concrete IGameAction
		Assert.AreEqual(1, effect.GameActions.Count, "Should have one GameAction");
		Assert.IsInstanceOfType(effect.GameActions[0], typeof(DamageAction), "Action type should be DamageAction");
		Assert.AreEqual(5, ((DamageAction)effect.GameActions[0]).Damage.GetValue(null, null), "Damage value should be 5");
	}


	[TestMethod]
	public void CreateTargetedSpellDefinitionTest()
	{
		// Arrange
		SpellCard card = new SpellCard("TestSpell_DealDamage", 1);
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
		Assert.AreEqual(CardType.Spell, spellDef.Type, "Type should be Spell");

		// Check that SpellCastEffects were serialized and deserialized correctly
		Assert.AreEqual(1, spellDef.SpellCastEffects.Count, "Should have one SpellCastEffect");

		var effect = spellDef.SpellCastEffects[0];
		Assert.IsNotNull(effect.GameActions, "Game actions should not be null");
		Assert.AreEqual(2, effect.GameActions.Count, "Should have two GameActions");

		Assert.IsInstanceOfType(effect.GameActions[0], typeof(DamageAction), "First action should be DamageAction");
		Assert.IsInstanceOfType(effect.GameActions[1], typeof(FreezeAction), "Second action should be FreezeAction");
		Assert.AreEqual(3, ((DamageAction)effect.GameActions[0]).Damage.GetValue(null, null), "Damage value should be 3");
	}

	[TestMethod]
	public void CreateWeaponDefinitionTest_RoundTrip()
	{
		WeaponCard card = new WeaponCard("TestSwordRoundTrip", cost: 3, attack: 4, durabilty: 2);

		var json = CardDatabase.CreateJsonFromWeaponCard(card, "SaveTestWeapon");
		var loadedCard = CardDatabase.LoadCardFromJson(json);

		Assert.IsNotNull(loadedCard, "Loaded card should not be null");
		Assert.IsInstanceOfType(loadedCard, typeof(WeaponCardDefinition), "Loaded card should be a WeaponCardDefinition");

		var weaponDef = (WeaponCardDefinition)loadedCard;

		Assert.AreEqual("SaveTestWeapon", weaponDef.Id, "Card Id should match");
		Assert.AreEqual(card.Name, weaponDef.Name, "Card name should match");
		Assert.AreEqual(card.ManaCost, weaponDef.Cost, "Mana cost should match");
		Assert.AreEqual(CardType.Weapon, weaponDef.Type, "Type should be Weapon");
		Assert.AreEqual(4, weaponDef.Attack, "Attack should match");
		Assert.AreEqual(2, weaponDef.Durability, "Durability should match");
	}

	[TestMethod]
	public void BuildMinionCard_FromInlineDefinition_MatchesGetMinionCard()
	{
		var def = new MinionCardDefinition
		{
			Type = CardType.Minion,
			Id = "InlineOnlyMinion",
			Name = "Inline Only Minion",
			Cost = 2,
			Attack = 3,
			Health = 4,
			Tribes = new List<string> { "Murloc" },
		};

		var cardDatabase = new CardDatabase(DBPath);
		var owner = new Player("Test");

		var card = cardDatabase.BuildMinionCard(def, owner);

		Assert.AreEqual("Inline Only Minion", card.Name);
		Assert.AreEqual(2, card.ManaCost);
		Assert.AreEqual(3, card.Attack);
		Assert.AreEqual(4, card.Health);
		Assert.IsTrue(TribeUtils.Matches(card.MinionTribes, "Murloc"));
		Assert.AreEqual(owner, card.Owner);
	}

	[TestMethod]
	public void DamageAction_SupportsNonConstantValueProvider_StatValue()
	{
		// Proves the functional gap in the old EmitParams/ConsumeParams scheme is fixed: DamageAction.Damage
		// can now be any IValueProvider (e.g. "deal damage equal to source's attack"), not just a constant int.
		var card = new MinionCard("StatValueBattlecryMinion", cost: 1, attack: 5, health: 1);
		card.TriggeredEffects.Add(new TriggeredEffect()
		{
			EffectTrigger = EffectTrigger.Battlecry,
			EffectTiming = EffectTiming.Post,
			GameActions = new List<IGameAction>()
			{
				new DamageAction()
				{
					Damage = new StatValue { EntityStat = Stat.Attack, EntityContextProvider = ContextProvider.Source },
				}
			},
		});

		var json = CardDatabase.CreateFileFromMinionCard(card, ".\\Data\\", "StatValueBattlecryMinion");
		var loadedCard = CardDatabase.LoadCardFromJson(json);

		Assert.IsInstanceOfType(loadedCard, typeof(MinionCardDefinition));
		var minionDef = (MinionCardDefinition)loadedCard;

		Assert.AreEqual(1, minionDef.TriggeredEffects.Count);
		var action = minionDef.TriggeredEffects[0].GameActions[0];
		Assert.IsInstanceOfType(action, typeof(DamageAction));
		Assert.IsInstanceOfType(((DamageAction)action).Damage, typeof(StatValue), "Damage should round-trip as a StatValue, not collapse to a constant");
	}
}
