namespace CardBattleEngine.Test;

[TestClass]
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
			TargetType = TargetType.AnyEnemy,
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
		Assert.AreEqual(TargetType.AnyEnemy, loadedMinion.TriggeredEffects[0].TargetType);
		Assert.AreEqual(TargetType.AnyEnemy, loadedMinion.TriggeredEffects[0].TargetType);
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
		Assert.AreEqual(TargetType.AnyEnemy, effect.TargetType);
		Assert.AreEqual(1, effect.GameActions.Count);
		Assert.IsInstanceOfType(effect.GameActions[0], typeof(DamageAction));
	}
}
