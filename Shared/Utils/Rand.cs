using System;

public sealed class Rand
{
	#region State

	private readonly Random Rng;

	public int Seed { get; }

	#endregion


	#region Construction

	public Rand(int seed)
	{
		Seed = seed;
		Rng = new Random(seed);
	}

	#endregion


	#region Integers

	public int NextInt()
	{
		return Rng.Next();
	}


	public int RangeInt(int min, int max)
	{
		return Rng.Next(min, max + 1);
	}

	#endregion


	#region Floating point

	public double NextDouble()
	{
		return Rng.NextDouble();
	}


	public float NextFloat()
	{
		return Rng.NextSingle();
	}


	public float RangeFloat(float min, float max)
	{
		return min + Rng.NextSingle() * (max - min);
	}


	public bool Chance(float probability)
	{
		return Rng.NextSingle() < probability;
	}

	#endregion
}
