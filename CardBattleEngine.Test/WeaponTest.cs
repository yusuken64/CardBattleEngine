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
		var engine = new GameEngine(new XorShiftRNG(1));

		var current = state.CurrentPlayer;
		var opponent = state.OpponentPlayer;

		var weapon = new Weapon
		{
			Attack = 3,
			Durability = 2
		};

		engine.Resolve(state, new ActionContext()
		{
			Target = current
		}, new EquipWeaponAction { Weapon = weapon });

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

}
