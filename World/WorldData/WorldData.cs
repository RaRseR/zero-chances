using Godot;
using System;
using System.Collections.Generic;

public class WorldData
{
	private WorldConfiguration Configuration;
	
	public PointArray<float> Heights { get; private set; }
	
	//public Region[] Regions { get; private set; }
	public Biome[] Biomes { get; private set; }
	
	public ushort[] PointRegionIds { get; private set; }
	public ushort[] PointBiomeIds { get; private set; }
	
	public uint[] PointDistanceToShore { get; private set; }

	public WorldData(WorldConfiguration configuration)
	{
		Heights = new HeightmapGenerator(configuration).GetHeightmap();
		
		
		
		//Regions = TerrainUtil.Generate(
			//(float)GraphWidth,
			//(float)GraphHeight,
			//Heights,
			//Positions,
			//Neighbours,
			//Polygons
		//);
	}
}
