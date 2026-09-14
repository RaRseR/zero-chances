using System;

public enum BlendMode
{
	Replace,
	Add,
	Max
}


public abstract class PrimitiveStep : Step
{
	#region Configuration

	private readonly float MinValue;
	private readonly float MaxValue;

	private readonly FalloffType Curve;

	private readonly BlendMode Mode;

	#endregion


	#region Construction

	protected PrimitiveStep(float minValue, float maxValue, FalloffType curve, BlendMode mode)
	{
		MinValue = minValue;
		MaxValue = maxValue;
		Curve = curve;
		Mode = mode;
	}

	#endregion


	#region Helpers

	protected float GetValue(Rand rand)
	{
		return rand.RangeFloat(MathF.Min(MinValue, MaxValue), MathF.Max(MinValue, MaxValue));
	}


	protected float GetWeight(float t)
	{
		return Falloff.Evaluate(t, Curve);
	}


	protected void Blend(float[] field, int index, float value, float weight)
	{
		float current = field[index];

		switch (Mode)
		{
			case BlendMode.Add:
				field[index] = current + value * weight;
				break;

			case BlendMode.Max:
				field[index] = MathF.Max(current, value * weight);
				break;

			default:
				field[index] = current + (value - current) * weight;
				break;
		}
	}

	#endregion
}
