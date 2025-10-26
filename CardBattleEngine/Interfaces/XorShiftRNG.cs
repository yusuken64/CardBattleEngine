namespace CardBattleEngine;

// Simple Xorshift RNG for speed and reproducibility
public sealed class XorShiftRNG : IRNG
{
	private ulong _state;
	public XorShiftRNG(ulong seed)
	{
		_state = seed == 0 ? 0xdeadbeefcafebabeUL : seed;
	}
	private ulong NextULong()
	{
		_state ^= _state << 13;
		_state ^= _state >> 7;
		_state ^= _state << 17;
		return _state;
	}
	public int NextInt(int maxExclusive)
	{
		if (maxExclusive <= 0) return 0;
		return (int)(NextULong() % (uint)maxExclusive);
	}
	public int NextInt(int minInclusive, int maxExclusive)
	{
		return minInclusive + NextInt(maxExclusive - minInclusive);
	}
	public double NextDouble()
	{
		return (NextULong() & (1UL << 53) - 1) / (double)(1UL << 53);
	}
	IRNG IRNG.Clone()
	{
		// Copy the internal state
		var clone = new XorShiftRNG(1); // temporary seed doesn't matter
		clone._state = this._state;
		return clone;
	}
}
