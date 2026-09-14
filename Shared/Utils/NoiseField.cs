using System;

public sealed class NoiseField
{
	#region State

	private readonly int Seed;

	#endregion


	#region Construction

	public NoiseField(int seed)
	{
		Seed = seed;
	}

	#endregion


	#region Sampling

	public float Sample(float x, float y)
	{
		int cellX = (int)MathF.Floor(x);
		int cellY = (int)MathF.Floor(y);

		float localX = x - cellX;
		float localY = y - cellY;

		float weightX = Smooth(localX);
		float weightY = Smooth(localY);

		float corner00 = Corner(cellX, cellY);
		float corner10 = Corner(cellX + 1, cellY);
		float corner01 = Corner(cellX, cellY + 1);
		float corner11 = Corner(cellX + 1, cellY + 1);

		float top = corner00 + (corner10 - corner00) * weightX;
		float bottom = corner01 + (corner11 - corner01) * weightX;

		return top + (bottom - top) * weightY;
	}


	public float Fbm(float x, float y, int octaves, float lacunarity, float gain)
	{
		float total = 0f;
		float amplitude = 1f;
		float frequency = 1f;
		float normalizer = 0f;

		for (int octave = 0; octave < octaves; octave++)
		{
			total += Sample(x * frequency + octave * 37.19f, y * frequency - octave * 21.71f) * amplitude;
			normalizer += amplitude;

			amplitude *= gain;
			frequency *= lacunarity;
		}

		return normalizer > 0f ? total / normalizer : 0f;
	}


	public float Ridged(float x, float y, int octaves, float lacunarity, float gain)
	{
		return 1f - MathF.Abs(Fbm(x, y, octaves, lacunarity, gain));
	}


	public (float x, float y) Warp(float x, float y, float amount)
	{
		float offsetX = Fbm(x + 11.3f, y - 7.1f, 3, 2f, 0.5f);
		float offsetY = Fbm(x - 5.7f, y + 19.4f, 3, 2f, 0.5f);

		return (x + offsetX * amount, y + offsetY * amount);
	}

	#endregion


	#region Helpers

	private float Corner(int x, int y)
	{
		uint value = (uint)Hash(Seed, x, y);

		return (value & 0xFFFF) / 32767.5f - 1f;
	}


	private static int Hash(int seed, int x, int y)
	{
		uint value = (uint)seed;

		value ^= (uint)x * 2654435761u;
		value = Mix(value);

		value ^= (uint)y * 2246822519u;
		value = Mix(value);

		return (int)(value & 0x7FFFFFFF);
	}


	private static uint Mix(uint value)
	{
		value ^= value >> 16;
		value *= 2246822507u;
		value ^= value >> 13;
		value *= 3266489909u;
		value ^= value >> 16;

		return value;
	}


	private static float Smooth(float t)
	{
		return t * t * t * (t * (t * 6f - 15f) + 10f);
	}

	#endregion
}
