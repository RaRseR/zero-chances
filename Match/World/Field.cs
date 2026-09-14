using System;
using System.Collections.Generic;

public enum TracePathMode
{
	Straight,
	Random,
	ByValue
}


public static class PointDirections
{
	public const int COUNT = 8;


	public static readonly int[] OFFSET_X =
	{
		-1,  0,  1,
		-1,      1,
		-1,  0,  1
	};

	public static readonly int[] OFFSET_Y =
	{
		-1, -1, -1,
		 0,      0,
		 1,  1,  1
	};
}


public static class Field
{
	#region Access

	public static int Index(int side, int x, int y)
	{
		return y * side + x;
	}


	public static bool IsInRange(int side, int x, int y)
	{
		return x >= 0 && y >= 0 && x < side && y < side;
	}


	public static void Fill(float[] field, float value)
	{
		for (int index = 0; index < field.Length; index++)
		{
			field[index] = value;
		}
	}

	#endregion


	#region Shapes

	public static (int x, int y)[] InRadius(int side, int centerX, int centerY, int radius)
	{
		List<(int, int)> points = new();

		int radiusSquared = radius * radius;

		int minX = Math.Max(0, centerX - radius);
		int maxX = Math.Min(side - 1, centerX + radius);

		int minY = Math.Max(0, centerY - radius);
		int maxY = Math.Min(side - 1, centerY + radius);

		for (int y = minY; y <= maxY; y++)
		{
			int dy = y - centerY;
			int dySquared = dy * dy;

			for (int x = minX; x <= maxX; x++)
			{
				int dx = x - centerX;

				if (dx * dx + dySquared <= radiusSquared)
				{
					points.Add((x, y));
				}
			}
		}

		return points.ToArray();
	}

	#endregion


	#region Paths

	public static (int x, int y)[] InPath(
		int side,
		Rand rand,
		int startX, int startY,
		int endX, int endY,
		TracePathMode mode = TracePathMode.Straight,
		int minLength = 1, int maxLength = int.MaxValue,
		float straightness = 0.6f
	)
	{
		switch (mode)
		{
			case TracePathMode.Straight:
				return TraceStraight(startX, startY, endX, endY, maxLength);

			case TracePathMode.Random:
				return TraceRandom(side, rand, startX, startY, endX, endY, minLength, maxLength, straightness);

			case TracePathMode.ByValue:
				return TraceStraight(startX, startY, endX, endY, maxLength);

			default:
				throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unhandled TracePath mode");
		}
	}


	private static (int x, int y)[] TraceStraight(
		int startX, int startY,
		int endX, int endY,
		int maxLength
	)
	{
		List<(int, int)> points = new() { (startX, startY) };

		int x = startX;
		int y = startY;

		while (!(x == endX && y == endY) && points.Count < maxLength)
		{
			x += Math.Sign(endX - x);
			y += Math.Sign(endY - y);

			points.Add((x, y));
		}

		return points.ToArray();
	}


	private static (int x, int y)[] TraceRandom(
		int side,
		Rand rand,
		int startX, int startY,
		int endX, int endY,
		int minLength, int maxLength,
		float straightness
	)
	{
		List<(int, int)> points = new() { (startX, startY) };
		HashSet<(int, int)> visited = new() { (startX, startY) };

		int x = startX;
		int y = startY;

		while (!(x == endX && y == endY) && points.Count < maxLength)
		{
			int remaining = maxLength - points.Count;
			int distanceToEnd = ChebyshevDistance(x, y, endX, endY);

			bool mustConverge = distanceToEnd >= remaining;
			bool minLengthMet = points.Count >= minLength;

			List<(int nx, int ny, float weight)> candidates = GatherCandidates(
				side, x, y, endX, endY, remaining, distanceToEnd, mustConverge, minLengthMet,
				visited, straightness, allowVisited: false, allowAnyDistance: false
			);

			if (candidates.Count == 0)
			{
				candidates = GatherCandidates(
					side, x, y, endX, endY, remaining, distanceToEnd, mustConverge, minLengthMet,
					visited, straightness, allowVisited: true, allowAnyDistance: false
				);
			}

			if (candidates.Count == 0)
			{
				candidates = GatherCandidates(
					side, x, y, endX, endY, remaining, distanceToEnd, mustConverge, minLengthMet,
					visited, straightness, allowVisited: true, allowAnyDistance: true
				);
			}

			if (candidates.Count == 0) break;

			(x, y) = WeightedPoint(rand, candidates);

			points.Add((x, y));
			visited.Add((x, y));
		}

		return points.ToArray();
	}


	private static List<(int nx, int ny, float weight)> GatherCandidates(
		int side,
		int x, int y,
		int endX, int endY,
		int remaining, int distanceToEnd,
		bool mustConverge, bool minLengthMet,
		HashSet<(int, int)> visited,
		float straightness,
		bool allowVisited,
		bool allowAnyDistance
	)
	{
		List<(int, int, float)> candidates = new();

		for (int index = 0; index < PointDirections.COUNT; index++)
		{
			int nextX = x + PointDirections.OFFSET_X[index];
			int nextY = y + PointDirections.OFFSET_Y[index];

			if (!IsInRange(side, nextX, nextY)) continue;
			if (!allowVisited && visited.Contains((nextX, nextY))) continue;

			int newDistance = ChebyshevDistance(nextX, nextY, endX, endY);

			if (!allowAnyDistance)
			{
				if (newDistance >= remaining - 1) continue;
				if (!minLengthMet && nextX == endX && nextY == endY) continue;
				if (mustConverge && newDistance >= distanceToEnd) continue;
			}

			float bias = mustConverge ? 1f : (1f - straightness) * 2f + 0.1f;
			float weight = 1f / (newDistance * bias + 1f);

			candidates.Add((nextX, nextY, weight));
		}

		return candidates;
	}


	private static (int x, int y) WeightedPoint(Rand rand, List<(int nx, int ny, float weight)> candidates)
	{
		float total = 0f;

		foreach ((int nx, int ny, float weight) candidate in candidates)
		{
			total += candidate.weight;
		}

		float roll = rand.RangeFloat(0f, total);
		float accumulated = 0f;

		foreach ((int nx, int ny, float weight) candidate in candidates)
		{
			accumulated += candidate.weight;

			if (roll <= accumulated) return (candidate.nx, candidate.ny);
		}

		return (candidates[^1].nx, candidates[^1].ny);
	}


	private static int ChebyshevDistance(int firstX, int firstY, int secondX, int secondY)
	{
		return Math.Max(Math.Abs(firstX - secondX), Math.Abs(firstY - secondY));
	}

	#endregion
}
