using CardBattleEngine;

var minionID = "Murloc";
var card = new MinionCard(minionID, cost: 1, attack: 1, health: 1);
card.MinionTribe = MinionTribe.Murloc;
card.TriggeredEffects.Add(new TriggeredEffect()
{
	EffectTrigger = EffectTrigger.SummonMinion,
	EffectTiming = EffectTiming.Pre,
	TargetType = TargetingType.Self,
	Condition = new SummonedMinionTribeCondition()
	{
		MinionTribe = MinionTribe.Murloc,
		MinionToMinionRelationship = MinionToMinionRelationship.Friendly
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

