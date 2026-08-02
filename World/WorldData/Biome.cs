using Godot;

public enum BiomeType
{
	
}

public partial class Biome
{
	public string Title;
	
	public ushort RegionId;
	public BiomeType Type;
	
	public uint Size;
	
	public Vector2 Centroid;
	public Vector2 MinBoundary;
	public Vector2 MaxBoundary;
	
	public Biome(string title, ushort regionId, BiomeType type, uint size, Vector2 centroid, Vector2 minBoundary, Vector2 maxBoundary)
	{
		Title = title;
		RegionId = regionId;
		Type = type;
		Size = size;
		Centroid = centroid;
		MinBoundary = minBoundary;
		MaxBoundary = maxBoundary;
	}
}
