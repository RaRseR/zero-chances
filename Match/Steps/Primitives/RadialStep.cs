using System;

public sealed class RadialStep : PrimitiveStep
{
	#region Configuration

	private readonly float MinX;
	private readonly float MinY;
	private readonly float MaxX;
	private readonly float MaxY;

	private readonly float MinRadius;
	private readonly float MaxRadius;

	private readonly float FlatFraction;
	private readonly float RingRadiusFraction;

	#endregion


	#region Construction

	public RadialStep(
		float minValue,
		float maxValue,
		float minX,
		float minY,
		float maxX,
		float maxY,
		float minRadius,
		float maxRadius,
		float flatFraction = 0f,
		float ringRadiusFraction = 0f,
		FalloffType curve = FalloffType.SmoothStep,
		BlendMode mode = BlendMode.Replace
	) : base(minValue, maxValue, curve, mode)
	{
		MinX = minX;
		MinY = minY;
		MaxX = maxX;
		MaxY = maxY;
		MinRadius = minRadius;
		MaxRadius = maxRadius;
		FlatFraction = flatFraction;
		RingRadiusFraction = ringRadiusFraction;
	}

	#endregion


	#region Execution

	public override void Execute(StepContext context)
	{
		float[] field = context.Layers.Height;
		int side = context.Side;
		Rand rand = context.Rand;

		float positionX = rand.RangeFloat(MinX, MaxX);
		float positionY = rand.RangeFloat(MinY, MaxY);
		float radiusFraction = rand.RangeFloat(MinRadius, MaxRadius);
		float value = GetValue(rand);

		int centerX = Math.Clamp((int)(positionX * (side - 1)), 0, side - 1);
		int centerY = Math.Clamp((int)(positionY * (side - 1)), 0, side - 1);
		int radiusPx = Math.Max(1, (int)(radiusFraction * side));

		int minX = Math.Max(0, centerX - radiusPx);
		int maxX = Math.Min(side - 1, centerX + radiusPx);
		int minY = Math.Max(0, centerY - radiusPx);
		int maxY = Math.Min(side - 1, centerY + radiusPx);

		for (int y = minY; y <= maxY; y++)
		{
			int row = y * side;

			for (int x = minX; x <= maxX; x++)
			{
				int dx = x - centerX;
				int dy = y - centerY;

				float t = MathF.Sqrt(dx * dx + dy * dy) / radiusPx;

				if (t > 1f) continue;

				float shapeT = t;

				if (RingRadiusFraction > 0f)
				{
					float denominator = MathF.Max(RingRadiusFraction, 1f - RingRadiusFraction);

					shapeT = MathF.Abs(t - RingRadiusFraction) / denominator;
				}

				float falloffT = shapeT <= FlatFraction
					? 0f
					: (shapeT - FlatFraction) / (1f - FlatFraction);

				Blend(field, row + x, value, GetWeight(falloffT));
			}
		}
	}

	#endregion
}
