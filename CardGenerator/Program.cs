using CardBattleEngine;

var testGame = GameFactory.CreateTestGame();
var current = testGame.Players[0];
var opponent = testGame.Players[1];

var minionID = "Murloc";
var card = new MinionCard(minionID, cost: 1, attack: 1, health: 1);
card.Owner = current;
card.MinionTribe = MinionTribe.Murloc;
card.TriggeredEffects.Add(new TriggeredEffect()
{
	EffectTrigger = EffectTrigger.SummonMinion,
	EffectTiming = EffectTiming.Post,
	TargetType = TargetType.AnyEnemy,
	Condition = new SummonedMinionTribeCondition()
	{
		MinionTribe = MinionTribe.Murloc
	},
	GameActions = new List<IGameAction>()
	{
		new AddStatModifierAction()
		{
			AttackChange = 1,
		}
	}
});

var json = CardDatabase.CreateFileFromMinionCard(card, ".\\Data\\", minionID);
Console.WriteLine(json);

