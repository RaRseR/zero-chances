public sealed class WorldLayers
{
	#region State

	public int Side { get; }

	public int PointCount { get; }

	public readonly float[] Height;
	public readonly float[] Temperature;
	public readonly float[] Moisture;
	public readonly byte[] Biome;
	public readonly byte[] Rock;
	public readonly int[] Resource;

	#endregion


	#region Construction

	public WorldLayers(int side)
	{
		Side = side;
		PointCount = side * side;

		Height = new float[PointCount];
		Temperature = new float[PointCount];
		Moisture = new float[PointCount];
		Biome = new byte[PointCount];
		Rock = new byte[PointCount];
		Resource = new int[PointCount];
	}

	#endregion


	#region Access

	public int Index(int x, int y)
	{
		return y * Side + x;
	}


	public bool IsInRange(int x, int y)
	{
		return x >= 0 && y >= 0 && x < Side && y < Side;
	}


	public float HeightAt(int x, int y)
	{
		return Height[y * Side + x];
	}


	public long ByteSize => (long)PointCount * (sizeof(float) * 3 + sizeof(int) + 2);

	#endregion
}
