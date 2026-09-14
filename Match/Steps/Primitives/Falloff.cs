using System;

public enum FalloffType
{
	Linear,
	SmoothStep,
	EaseOut,
	Gaussian
}


public static class Falloff
{
	#region Evaluation

	public static float Evaluate(float t, FalloffType type)
	{
		t = Math.Clamp(t, 0f, 1f);

		switch (type)
		{
			case FalloffType.Linear:
				return 1f - t;

			case FalloffType.SmoothStep:
				return 1f - (t * t * (3f - 2f * t));

			case FalloffType.EaseOut:
				return (1f - t) * (1f - t);

			case FalloffType.Gaussian:
				return StableExp(-(t * t) * 4.5f);

			default:
				return 1f - t;
		}
	}


	public static float StableExp(float x)
	{
		float value = 1f + x / 64f;

		value *= value;
		value *= value;
		value *= value;
		value *= value;
		value *= value;
		value *= value;

		return value < 0f ? 0f : value;
	}

	#endregion
}
