namespace CardBattleEngine;

public interface IRNG
{
	int NextInt(int maxExclusive);
	int NextInt(int minInclusive, int maxExclusive);
	double NextDouble();
}
