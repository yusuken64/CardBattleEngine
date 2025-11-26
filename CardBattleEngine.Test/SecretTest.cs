namespace CardBattleEngine.Test;

[TestClass]
public class SecretTest
{
	[TestMethod]
	public void CounterSpellTest()
	{
		// Arrange
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine(new XorShiftRNG(1));
		var player1 = state.Players[0];
		var player2 = state.Players[1];

		player1.Mana = 1;
		player2.Mana = 2;

		var secretCard = new SpellCard("CounterSpell", 1);
		secretCard.SpellCastEffects.Add(
			new SpellCastEffect()
			{
				GameActions = new List<IGameAction>()
				{
					new SecretAction()
					{
						Secret = new Secret()
						{
							SecretTrigger = new TriggeredEffect()
							{
								EffectTrigger = EffectTrigger.SpellCast,
								EffectTiming = EffectTiming.Pre,
								TargetType = TargetingType.None,
								GameActions = [new CancelEffectAction()],
								Condition= new SpellOwnerCondition()
								{
									TargetSide = TargetSide.Enemy
								},
								AffectedEntitySelector = new ContextSelector()
								{
									IncludeSourcePlayer = true,
								}
							}
						}
					}
				}
			});
		secretCard.Owner = player1;
		player1.Hand.Add(secretCard);

		var action = new PlayCardAction()
		{
			Card = secretCard,
		};
		ActionContext actionContext = new()
		{
			SourcePlayer = player1,
			SourceCard = secretCard,
			Target = player1,
		};

		Assert.IsTrue(action.IsValid(state, actionContext));
		engine.Resolve(state, actionContext, action);

		Assert.AreEqual(1, player1.Secrets.Count, "Secret should be active after playing CounterSpell.");

		// Define an enemy spell that would normally succeed
		var fireball = new SpellCard("Fireball", 2);
		fireball.SpellCastEffects.Add(
			new SpellCastEffect()
			{
				GameActions = new List<IGameAction>()
				{
				new DamageAction() { Damage = (Value)5 }
				}
			});

		var playFireball = new PlayCardAction()
		{
			Card = fireball,
		};
		playFireball.Card.Owner = player2;
		player2.Hand.Add(fireball);

		// Act — enemy casts a spell
		Assert.IsTrue(playFireball.IsValid(state, actionContext));
		engine.Resolve(state, new ActionContext()
		{
			SourcePlayer = player2,
			Source = player2,
			SourceCard = fireball,
			Target = player1
		}, playFireball);

		// Assert — spell should be canceled, Counterspell should trigger and disappear
		Assert.AreEqual(0, player1.Secrets.Count, "CounterSpell should trigger and be consumed.");
	}
}
