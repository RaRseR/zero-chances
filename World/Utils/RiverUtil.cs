using Godot;
using System;

public readonly struct River 
{
	public readonly uint Source;
	public readonly uint Mouth;
	public readonly uint Parent;
	public readonly uint Basin;
	public readonly uint Length;
	public readonly uint Discharge;
	public readonly float Width;
	public readonly float WidthFactor;
	public readonly float SourceWidth;
	public readonly string Title;
	public readonly uint[] CellIds;
}

public static class RiverUtil
{
	private const int FLUX_FACTOR = 500;
	
	public static void Generate(float[] heights, Vector2[] positions, uint[][] neighbours)
	{
		
	}
}
