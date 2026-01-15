using System.Net.Http.Headers;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Security;

namespace CardBattleEngine.Test;

[TestClass]
public class WeaponTest
{
	[TestMethod]
	public void EquipWeaponTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);

		var weapon = new Weapon("test", 3, 2);

		engine.Resolve(state, new ActionContext()
		{
			Target = current
		}, new AcquireWeaponAction { Weapon = weapon });

		Assert.AreEqual(weapon, current.EquippedWeapon);
		Assert.AreEqual(weapon.Attack, current.Attack);
		Assert.IsTrue(current.CanAttack());

		var initialOpponentHealth = opponent.Health;
		var initialDurability = weapon.Durability;

		engine.Resolve(state,
			new ActionContext
			{
				SourcePlayer = current,
				Source = current,
				Target = opponent,
			},
			new AttackAction());

		Assert.AreEqual(initialOpponentHealth - 3, opponent.Health);
		Assert.AreEqual(initialDurability - 1, current.EquippedWeapon.Durability);
		Assert.IsFalse(current.CanAttack());
	}

	[TestMethod]
	public void ReEquipWeaponTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var current = state.CurrentPlayer;
		var opponent = state.OpponentOf(current);

		var weapon = new Weapon("test", 3, 2)
		{
			Attack = 3,
			Durability = 2
		};

		var weapon2 = new Weapon("test", 1, 2)
		{
			Attack = 1,
			Durability = 2
		};

		engine.Resolve(state, new ActionContext()
		{
			Target = current
		}, new AcquireWeaponAction { Weapon = weapon });

		Assert.AreSame(weapon, current.EquippedWeapon);
		Assert.AreEqual(3, current.EquippedWeapon.Attack);
		Assert.AreEqual(2, current.EquippedWeapon.Durability);
		Assert.AreEqual(3, current.Attack);

		engine.Resolve(state, new ActionContext()
		{
			Target = current
		}, new AcquireWeaponAction { Weapon = weapon2 });

		Assert.AreSame(weapon2, current.EquippedWeapon, "The second weapon should now be equipped.");
		Assert.AreEqual(1, current.EquippedWeapon.Attack, "Attack value should match the new weapon.");
		Assert.AreEqual(2, current.EquippedWeapon.Durability, "Durability should match the new weapon.");
		Assert.AreNotSame(weapon, current.EquippedWeapon, "Old weapon should be unequipped.");
	}

	[TestMethod]
	public void WeaponDurabilityTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var attacker = state.CurrentPlayer;
		var defender = state.OpponentOf(attacker);

		bool deathrattleTriggered = false;

		var weapon = new Weapon("test", 1, 2)
		{
			Attack = 1,
			Durability = 2,
			TriggeredEffects = new List<TriggeredEffect>()
			{
				new TriggeredEffect()
				{
					EffectTiming = EffectTiming.Post,
					EffectTrigger = EffectTrigger.Deathrattle,
					GameActions = new List<IGameAction>()
					{
						new DebugLambaAction()
						{
							IsValidFunc = (state, action) => true,
							ResolveFunc = (state, action) =>
							{
								deathrattleTriggered = true;
								return [];
							}
						}
					}
				}
			}
		};

		// Equip the weapon
		engine.Resolve(state, new ActionContext { Target = attacker }, new AcquireWeaponAction { Weapon = weapon });

		// Dummy target to attack
		MinionCard card = new MinionCard("Defender", 1, 2, 5);
		var targetMinion = new Minion(card, defender);
		defender.Board.Add(targetMinion);

		// Attack 1
		attacker.HasAttackedThisTurn = false;
		engine.Resolve(state, new ActionContext()
		{
			Source = attacker,
			Target = defender,
		}, new AttackAction());

		Assert.AreEqual(1, weapon.Durability, "Weapon durability should reduce to 1 after first attack.");
		Assert.AreSame(weapon, attacker.EquippedWeapon, "Weapon should still be equipped after first attack.");
		Assert.IsFalse(deathrattleTriggered, "Deathrattle should not trigger until the weapon breaks.");

		// Attack 2
		attacker.HasAttackedThisTurn = false;
		engine.Resolve(state, new ActionContext()
		{
			Source = attacker,
			Target = defender,
		}, new AttackAction());

		//Assert.AreEqual(0, weapon.Durability, "Weapon durability should reach 0 after second attack.");
		//Assert.IsTrue(weapon.IsDestroyed, "Weapon should be marked as destroyed after breaking.");
		Assert.IsNull(attacker.EquippedWeapon, "Weapon should no longer be equipped after breaking.");
		Assert.IsTrue(deathrattleTriggered, "Weapon deathrattle should trigger when it breaks.");
	}

	[TestMethod]
	public void WeaponBuffTest()
	{
		var state = GameFactory.CreateTestGame();
		var engine = new GameEngine();

		var attacker = state.CurrentPlayer;
		var defender = state.OpponentOf(attacker);

		var weapon = new Weapon("test", 1, 2)
		{
			Attack = 1,
			Durability = 2,
		};
		var current = state.CurrentPlayer;

		current.EquipWeapon(weapon);

		var spell = new SpellCard("deadly poison", 1);
		spell.SpellCastEffects.Add(new SpellCastEffect()
		{
			GameActions = [new AddStatModifierAction() {
				AttackChange = (Value)2
			}],
			AffectedEntitySelector = new TargetOperationSelector()
			{
				Operations = [ new SelectWeaponOperation()
				{
					Side = TeamRelationship.Friendly
				}],
				ResolutionTiming = TargetResolutionTiming.Once
			}
		});
		spell.Owner = current;
		spell.Owner.Hand.Add(spell);

		current.Mana = 1;

		Assert.AreEqual(1, current.Attack);

		engine.Resolve(
			state,
			new ActionContext()
			{
				Source = current,
				SourcePlayer = current,
			},
			new PlayCardAction()
			{
				Card = spell
			});

		Assert.AreEqual(3, current.Attack);
	}
}
