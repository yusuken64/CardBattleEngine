using CardBattleEngine;

var testGame = GameFactory.CreateTestGame();
var current = testGame.Players[0];
var opponent = testGame.Players[1];

var minionID = "TestMinion";
var card = new MinionCard(minionID, cost: 1, attack: 1, health: 1);
card.Owner = current;
//card.TriggeredEffects.Add(new TriggeredEffect()
//{
//	EffectTrigger = EffectTrigger.Battlecry,
//	EffectTiming = EffectTiming.Post,
//	TargetType = TargetType.AnyEnemy,
//	GameActions = new List<IGameAction>()
//	{
//		new DamageAction() { Damage = 1 }
//	}
//});

var json = CardDatabase.CreateFileFromMinionCard(card, ".\\Data\\", minionID);
Console.WriteLine(json);

