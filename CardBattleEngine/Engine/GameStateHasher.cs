namespace CardBattleEngine;

public static class GameStateHasher
{
	public static ulong HashState(GameState s, Player me)
	{
		var enemy = s.OpponentOf(me);

		ulong hash = 1469598103934665603UL; // FNV offset

		void HashHero(Player p)
		{
			hash = HashU64(hash, (uint)p.Health);
			hash = HashU64(hash, (uint)p.MaxHealth);
			hash = HashU64(hash, (uint)p.Mana);
			hash = HashU64(hash, (uint)p.MaxMana);
			hash = HashU64(hash, (uint)p.Attack);
			hash = HashU64(hash, (uint)p.Hand.Count);
			
			uint heroFlags = 0;
			if (p.CanAttack()) heroFlags |= 1 << 0;
			if (p.IsFrozen) heroFlags |= 1 << 1;
			if (p.EquippedWeapon != null) heroFlags |= 1 << 2;

			hash = HashU64(hash, heroFlags);

			foreach (var m in CanonicalBoard(p.Board))
			{
				hash = HashU64(hash, PackMinion(m));
			}
		}

		HashHero(me);
		hash = HashU64(hash, 0xFFFFFFFF); // separator
		HashHero(enemy);

		hash = HashU64(hash, s.CurrentPlayer == me ? 1u : 2u);

		return hash;
	}

	static ulong HashU64(ulong hash, ulong data)
	{
		const ulong FNV_PRIME = 1099511628211;
		hash ^= data;
		return hash * FNV_PRIME;
	}

	static IEnumerable<Minion> CanonicalBoard(IEnumerable<Minion> board)
	{
		return board.OrderByDescending(m => m.Attack)
					.ThenByDescending(m => m.Health)
					.ThenByDescending(m =>
						(m.Taunt ? 1 : 0) |
						(m.HasDivineShield ? 2 : 0) |
						(m.HasPoisonous ? 4 : 0) |
						(m.HasWindfury ? 8 : 0));
	}

	static ulong PackMinion(Minion m)
	{
		ushort mask = 0;
		if (m.Taunt) mask |= 1 << 0;
		if (m.HasDivineShield) mask |= 1 << 1;
		if (m.HasPoisonous) mask |= 1 << 2;
		if (m.HasWindfury) mask |= 1 << 3;
		if (m.IsStealth) mask |= 1 << 4;
		if (m.HasLifeSteal) mask |= 1 << 5;
		if (m.HasReborn) mask |= 1 << 6;
		if (m.IsFrozen) mask |= 1 << 7;
		if (m.AttackBehavior.CanInitiateAttack(m, out _)) mask |= 1 << 8;
		
		ushort cardId = m.CardNumericId; // assigned from registry

		return
			(ulong)m.Attack |
			((ulong)m.Health << 8) |
			((ulong)m.MaxHealth << 16) |
			((ulong)mask << 24) |
			((ulong)cardId << 40);
	}

	const int MAX_MINIONS = 7;

	static void AddHeroFeatures(List<float> v, Player p)
	{
		v.Add(p.Health / 30f);
		v.Add(p.Mana / 10f);
		v.Add(p.MaxMana / 10f);
		v.Add((p.EquippedWeapon?.Attack ?? 0) / 10f);
		v.Add(p.Hand.Count / 10f);
		v.Add(p.CanAttack() ? 1f : 0f);
	}

	static void AddMinion(List<float> v, Minion m)
	{
		v.Add(m.Attack / 20f);
		v.Add(m.Health / 20f);
		v.Add(m.MaxHealth / 20f);

		bool readyNextTurn = !m.HasSummoningSickness && !m.IsFrozen;
		v.Add(readyNextTurn ? 1f : 0f);

		v.Add(m.Taunt ? 1 : 0);
		v.Add(m.HasDivineShield ? 1 : 0);
		v.Add(m.HasPoisonous ? 1 : 0);
		v.Add(m.HasWindfury ? 1 : 0);
		v.Add(m.IsStealth ? 1 : 0);
		v.Add(m.HasLifeSteal ? 1 : 0);
		v.Add(m.HasReborn ? 1 : 0);
	}

	static void AddBoard(List<float> v, IEnumerable<Minion> board)
	{
		var ordered = CanonicalBoard(board).Take(MAX_MINIONS).ToList();

		foreach (var m in ordered)
			AddMinion(v, m);

		for (int i = ordered.Count; i < MAX_MINIONS; i++)
			for (int j = 0; j < 11; j++)
				v.Add(0);
	}

	public static float[] EncodeStateNN(GameState s, Player me)
	{
		var enemy = s.OpponentOf(me);
		var v = new List<float>(167);

		AddHeroFeatures(v, me);
		AddHeroFeatures(v, enemy);

		AddBoard(v, me.Board);
		AddBoard(v, enemy.Board);

		v.Add(s.CurrentPlayer == me ? 1f : 0f);

		return v.ToArray();
	}
}
