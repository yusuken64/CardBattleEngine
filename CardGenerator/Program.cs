using CardBattleEngine;

//var minionID = "TestMurloc";
//var card = new MinionCard(minionID, cost: 1, attack: 1, health: 1);
//card.MinionTribes = [MinionTribe.Murloc];
//card.TriggeredEffects.Add(new TriggeredEffect()
//{
//	EffectTrigger = EffectTrigger.SummonMinion,
//	EffectTiming = EffectTiming.Pre,
//	TargetType = TargetingType.Self,
//	Condition = new SummonedMinionTribeCondition()
//	{
//		MinionTribe = MinionTribe.Murloc,
//		MinionToMinionRelationship = MinionToMinionRelationship.Friendly
//	},
//	GameActions = new List<IGameAction>()
//	{
//		new AddStatModifierAction()
//		{
//			AttackChange = 1,
//		}
//	},
//	AffectedEntitySelector = new ContextSelector()
//	{
//		IncludeSource = true,
//	}
//});

var cardID = "CardID";
var card = new SpellCard("FlameStrike", 5)
{
	TargetingType = TargetingType.None,
};
card.SpellCastEffects.Add(new SpellCastEffect()
{
	GameActions = [new DamageAction()
	{
		Damage = (Value)5
	}],
	AffectedEntitySelector = new TargetOperationSelector()
	{
		Operations = [new SelectBoardEntitiesOperation() {
			Group = TargetGroup.Minions,
			Side = TeamRelationship.Enemy
		}]
	}
});

var json = CardDatabase.CreateJsonFromSpellCard(card, cardID);
Console.WriteLine(json);

