public abstract record WorldSizeFeature : WorldFeature
{
	public int ChunkCountPerWorldSide { get; init; }
	public int MaxTotalFeatures { get; init; }
	public int MaxFactionCount { get; init; }
}

public sealed record TinySizeFeature : WorldSizeFeature
{
	public TinySizeFeature()
	{ 
		ChunkCountPerWorldSide = 10;
		MaxTotalFeatures = 10;
		MaxFactionCount = 2; 
	}
}

public sealed record SmallSizeFeature : WorldSizeFeature
{
	public SmallSizeFeature() 
	{ 
		ChunkCountPerWorldSide = 25; 
		MaxTotalFeatures = 10;
		MaxFactionCount = 4;
	}
}

public sealed record StandardSizeFeature : WorldSizeFeature
{
	public StandardSizeFeature() 
	{ 
		ChunkCountPerWorldSide = 50; 
		MaxTotalFeatures = 10;
		MaxFactionCount = 8; 
	}
}

public sealed record LargeSizeFeature : WorldSizeFeature
{
	public LargeSizeFeature() 
	{ 
		ChunkCountPerWorldSide = 75; 
		MaxTotalFeatures = 10;
		MaxFactionCount = 16; 
	}
}

public sealed record HugeSizeFeature : WorldSizeFeature
{
	public HugeSizeFeature() 
	{ 
		ChunkCountPerWorldSide = 100; 
		MaxTotalFeatures = 10;
		MaxFactionCount = 32; 
	}
}
