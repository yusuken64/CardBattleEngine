using CardBattleEngine;

var testGame = GameFactory.CreateTestGame();
var current = testGame.Players[0];
var opponent = testGame.Players[1];

var card = new MinionCard("BattlecryMinion", cost: 1, attack: 1, health: 1);
card.Owner = current;
card.TriggeredEffect.Add(new TriggeredEffect()
{
	EffectTrigger = EffectTrigger.Battlecry,
	EffectTiming = EffectTiming.Post,
	TargetType = TargetType.AnyEnemy,
	GameActions = new List<IGameAction>()
	{
		new DamageAction() { Target = opponent, Damage = 1 }
	}
});

CardDatabase.CreateFileFromMinionCard(card, ".\\Data\\", "BattleCryMinion.json");