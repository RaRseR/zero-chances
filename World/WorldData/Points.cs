using Godot;
using System;
using System.Collections.Generic;

public partial class PointArray<T>
{
	private T[] Array;
	
	private int PointCountPerWolrdSide { get; }
	
	public PointArray(int pointCountPerWolrdSide)
	{
		PointCountPerWolrdSide = pointCountPerWolrdSide;

		Array = new T[PointCountPerWolrdSide * PointCountPerWolrdSide];
	}
	
	public T this[int x, int y]
	{
		get
		{
			CheckOutOfRange(x, y);
			return Array[GetIndex(x, y)]; 
		}
		set
		{ 
			CheckOutOfRange(x, y);
			Array[GetIndex(x, y)] = value; 
		}
	}
	
	private void CheckOutOfRange(int x, int y)
	{
		if (x >= PointCountPerWolrdSide || y >= PointCountPerWolrdSide)
		{
			throw new IndexOutOfRangeException($"[IndexOutOfRange]|[Index: ({x}, {y})][Range: {PointCountPerWolrdSide}]");
		}
	}
	
	private int GetIndex(int x, int y)
	{
		return (int)(y * PointCountPerWolrdSide + x);
	}
	
	public (int x, int y)[] TracePath(
		int startX, int startY,
		int endX, int endY,
		TracePathMode mode = TracePathMode.Straight,
		int minLength = 1, int maxLength = int.MaxValue,
		Func<T, float> valueSelector = null
	)
	{
		switch (mode) 
		{
			case TracePathMode.Straight:
				return TracePathStraight(
					startX, startY,
					endX, endY, 
					maxLength
				);
			case TracePathMode.Random:
				return TracePathRandom(
					startX, startY, 
					endX, endY, 
					minLength, maxLength
				);
			case TracePathMode.ByValue:
				return TracePathByValue(
					startX, startY, 
					endX, endY, 
					minLength, maxLength,
					valueSelector
				);
			default:
				throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unhandled TracePath mode");
		}
	}
	
	private (int x, int y)[] TracePathStraight(
		int startX, int startY,
		int endX, int endY,
		int maxLength
	)
	{
		List<(int, int)> points = new(){ (startX, startY) };
		
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
	
	private (int x, int y)[] TracePathRandom(
		int startX, int startY,
		int endX, int endY,
		int minLength, int maxLength
	)
	{
		List<(int, int)> points = new(){ (startX, startY) };
		HashSet<(int, int)> visited = new(){ (startX, startY) };
		
		int x = startX;
		int y = startY;
		
		//while (!(x == endX && y == endY) && points.Count < maxLength)
		//{
			//int remaining = maxLength - points.Count;
			//int distToEnd = ChebyshevDistance(x, y, endX, endY);
			//bool mustConverge = distToEnd >= remaining;
			//bool minLengthMet = points.Count >= minLength;
//
			//List<(int nx, int ny, float weight)> candidates = new();
//
			//for (int i = 0; i < PointDirections.Count; i++)
			//{
				//int nx = x + PointDirections.OffsetX[i];
				//int ny = y + PointDirections.OffsetY[i];
//
				//if (!IsInRange(nx, ny)) continue;
				//if (visited.Contains((nx, ny))) continue;
//
				//int newDist = ChebyshevDistance(nx, ny, endX, endY);
//
				//if (newDist >= remaining) continue;
				//if (!minLengthMet && nx == endX && ny == endY) continue;
				//if (mustConverge && newDist >= distToEnd) continue;
//
				//float bias = mustConverge ? 1f : 0.25f;
				//float weight = 1f / (newDist * bias + 1f);
//
				//candidates.Add((nx, ny, weight));
			//}
//
			//if (candidates.Count == 0) break;
//
			//(x, y) = WeightedRandomPoint(candidates);
			//points.Add((x, y));
			//visited.Add((x, y));
		//}
		
		return points.ToArray();
	}
	
	private (int x, int y)[] TracePathByValue(
		int startX, int startY,
		int endX, int endY,
		int minLength, int maxLength,
		Func<T, float> valueSelector
	)
	{
		List<(int, int)> points = new() { (startX, startY) };
		
		return points.ToArray();
	}
};

public static class PointDirections
{
	public const int Count = 8;
	
	public static readonly int[] OffsetX =
	{
		-1,  0,  1,
		-1,      1,
		-1,  0,  1
	};

	public static readonly int[] OffsetY =
	{
		-1, -1, -1,
		 0,      0,
		 1,  1,  1
	};
}

public enum TracePathMode
{
	Straight,
	Random,
	ByValue
}
