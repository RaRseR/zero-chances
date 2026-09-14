using System;
using System.Collections.Generic;

public sealed class LineStep : PrimitiveStep
{
	#region Configuration

	private readonly float StartMinX;
	private readonly float StartMinY;
	private readonly float StartMaxX;
	private readonly float StartMaxY;

	private readonly float EndMinX;
	private readonly float EndMinY;
	private readonly float EndMaxX;
	private readonly float EndMaxY;

	private readonly float MinWidth;
	private readonly float MaxWidth;

	private readonly float Straightness;
	private readonly float Asymmetry;

	#endregion


	#region Construction

	public LineStep(
		float minValue,
		float maxValue,
		float startMinX,
		float startMinY,
		float startMaxX,
		float startMaxY,
		float endMinX,
		float endMinY,
		float endMaxX,
		float endMaxY,
		float minWidth,
		float maxWidth,
		float straightness = 1f,
		float asymmetry = 0f,
		FalloffType curve = FalloffType.SmoothStep,
		BlendMode mode = BlendMode.Replace
	) : base(minValue, maxValue, curve, mode)
	{
		StartMinX = startMinX;
		StartMinY = startMinY;
		StartMaxX = startMaxX;
		StartMaxY = startMaxY;
		EndMinX = endMinX;
		EndMinY = endMinY;
		EndMaxX = endMaxX;
		EndMaxY = endMaxY;
		MinWidth = minWidth;
		MaxWidth = maxWidth;
		Straightness = straightness;
		Asymmetry = asymmetry;
	}

	#endregion


	#region Execution

	public override void Execute(StepContext context)
	{
		float[] field = context.Layers.Height;
		int side = context.Side;
		Rand rand = context.Rand;

		float startXFraction = rand.RangeFloat(StartMinX, StartMaxX);
		float startYFraction = rand.RangeFloat(StartMinY, StartMaxY);
		float endXFraction = rand.RangeFloat(EndMinX, EndMaxX);
		float endYFraction = rand.RangeFloat(EndMinY, EndMaxY);

		int startX = Math.Clamp((int)(startXFraction * (side - 1)), 0, side - 1);
		int startY = Math.Clamp((int)(startYFraction * (side - 1)), 0, side - 1);
		int endX = Math.Clamp((int)(endXFraction * (side - 1)), 0, side - 1);
		int endY = Math.Clamp((int)(endYFraction * (side - 1)), 0, side - 1);

		float widthFraction = rand.RangeFloat(MinWidth, MaxWidth);
		int widthPx = Math.Max(1, (int)(widthFraction * side));

		float value = GetValue(rand);

		TracePathMode mode = Straightness >= 1f ? TracePathMode.Straight : TracePathMode.Random;

		(int x, int y)[] path = Field.InPath(
			side, rand, startX, startY, endX, endY, mode, straightness: Straightness
		);

		Dictionary<(int x, int y), float> weightByPoint = new();

		for (int index = 0; index < path.Length; index++)
		{
			(int pointX, int pointY) = path[index];

			(int dirX, int dirY) = GetPathDirection(path, index);

			float dirLength = MathF.Sqrt(dirX * dirX + dirY * dirY);
			float perpendicularX = dirLength > 0f ? -dirY / dirLength : 0f;
			float perpendicularY = dirLength > 0f ? dirX / dirLength : 0f;

			int minX = Math.Max(0, pointX - widthPx);
			int maxX = Math.Min(side - 1, pointX + widthPx);
			int minY = Math.Max(0, pointY - widthPx);
			int maxY = Math.Min(side - 1, pointY + widthPx);

			for (int y = minY; y <= maxY; y++)
			{
				for (int x = minX; x <= maxX; x++)
				{
					int dx = x - pointX;
					int dy = y - pointY;

					float t = MathF.Sqrt(dx * dx + dy * dy) / widthPx;

					if (t > 1f) continue;

					float sideValue = dx * perpendicularX + dy * perpendicularY;
					float sideBias = MathF.Max(0f, 1f + Asymmetry * MathF.Sign(sideValue));

					float weight = GetWeight(t) * sideBias;

					(int x, int y) key = (x, y);

					if (!weightByPoint.TryGetValue(key, out float existing) || weight > existing)
					{
						weightByPoint[key] = weight;
					}
				}
			}
		}

		foreach (KeyValuePair<(int x, int y), float> pair in weightByPoint)
		{
			(int x, int y) = pair.Key;

			Blend(field, y * side + x, value, pair.Value);
		}
	}


	private static (int x, int y) GetPathDirection((int x, int y)[] path, int index)
	{
		if (path.Length == 1)
		{
			return (1, 0);
		}

		if (index < path.Length - 1)
		{
			return (path[index + 1].x - path[index].x, path[index + 1].y - path[index].y);
		}

		return (path[index].x - path[index - 1].x, path[index].y - path[index - 1].y);
	}

	#endregion
}
