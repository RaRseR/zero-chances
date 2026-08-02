using System;

public static class Rand
{
	private static Random Rng = new Random();

	public static void SetSeed(int seed)
	{
		Rng = new Random(seed);
	}
	
	public static int NextInt()
	{
		return Rng.Next(0, 1);
	}

	public static int RangeInt(int min, int max)
	{
		return Rng.Next(min, max + 1);
	}
	
	public static double NextDouble()
	{
		return Rng.NextDouble();
	}
	
	public static float NextFloat()
	{
		return Rng.NextSingle();
	}
	
	public static float RangeFloat(float min, float max)
	{
		return min + Rng.NextSingle() * (max - min);
	}
}
