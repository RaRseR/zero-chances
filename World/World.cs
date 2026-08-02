using Godot;
using System;
using System.Collections.Generic;

public partial class World : Node3D
{
	#region Constants
	
	private const float DistanceBetweenPoints = 100f;

	private const float WaterDepthFlatten = 0.3f;

	#endregion


	#region Fields

	public int Seed;
	private WorldConfiguration Configuration;
	private WorldData Data;

	private MeshInstance3D Terrain;
	private MeshInstance3D Water;
	
	private Camera CameraInstance;

	#endregion

	#region Godot Callbacks

	public override void _Ready()
	{
		Seed = (int)GD.Randi();
		Rand.SetSeed(Seed);
		
		Configuration = new WorldConfiguration(
			WorldSize.Tiny, 
			WorldTemplate.Island
		);

		Data = new WorldData(Configuration);
		
		SetupCamera();

		BuildTerrain();
		//BuildWater();
	}

	#endregion
	
	private void SetupCamera()
	{
		CameraInstance = GetNode<Camera>("Camera");
		
		float worldSide = Configuration.PointCountPerWorldSide * DistanceBetweenPoints;
		Vector3 worldCenter = new(worldSide / 2f, 0f, worldSide / 2f);
		CameraInstance.StartOrbitCenter = worldCenter;
		
		float worldSidePow = MathF.Pow(worldSide, 2f);
		CameraInstance.Far = MathF.Sqrt(worldSidePow + worldSidePow);
	}
	
	
	#region Building
	
	private void BuildTerrain()
	{
		int side = Configuration.PointCountPerWorldSide;

		SurfaceTool surface = new();
		surface.Begin(Mesh.PrimitiveType.Triangles);

		for (int y = 0; y < side - 1; y++)
		{
			for (int x = 0; x < side - 1; x++)
			{
				AddQuad(surface, x, y);
			}
		}

		surface.GenerateNormals();

		StandardMaterial3D material = new();
		material.VertexColorUseAsAlbedo = true;
		material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;

		MeshInstance3D Terrain = new();
		Terrain.Name = "Terrain";
		Terrain.Mesh = surface.Commit();
		Terrain.MaterialOverride = material;

		AddChild(Terrain);
	}


	private void AddQuad(SurfaceTool surface, int x, int y)
	{
		Vector3 v00 = ToWorld(x, y);
		Vector3 v10 = ToWorld(x + 1, y);
		Vector3 v01 = ToWorld(x, y + 1);
		Vector3 v11 = ToWorld(x + 1, y + 1);

		Color c00 = GetTerrainColor(Data.Heights[x, y]);
		Color c10 = GetTerrainColor(Data.Heights[x + 1, y]);
		Color c01 = GetTerrainColor(Data.Heights[x, y + 1]);
		Color c11 = GetTerrainColor(Data.Heights[x + 1, y + 1]);

		surface.SetColor(c00); surface.AddVertex(v00);
		surface.SetColor(c10); surface.AddVertex(v10);
		surface.SetColor(c11); surface.AddVertex(v11);

		surface.SetColor(c00); surface.AddVertex(v00);
		surface.SetColor(c11); surface.AddVertex(v11);
		surface.SetColor(c01); surface.AddVertex(v01);
	}


	private void BuildWater()
	{
		int side = Configuration.PointCountPerWorldSide;

		var plane = new PlaneMesh();
		plane.Size = new Vector2(side, side);

		var material = new StandardMaterial3D();
		material.AlbedoColor = new Color(0.12f, 0.35f, 0.55f, 0.75f);
		material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		material.Metallic = 0.3f;
		material.Roughness = 0.15f;

		MeshInstance3D Water = new();
		Water.Name = "Water";
		Water.Mesh = plane;
		Water.MaterialOverride = material;
		Water.Position = new Vector3(side / 2f, GetWorldHeight(Configuration.SeaLevel), side / 2f);

		AddChild(Water);
	}

	#endregion


	#region Helpers

	private Vector3 ToWorld(int x, int y)
	{
		return new Vector3(x * DistanceBetweenPoints, GetWorldHeight(Data.Heights[x, y]), y * DistanceBetweenPoints);
	}


	private float GetWorldHeight(float height)
	{
		if (height < Configuration.SeaLevel)
		{
			float depth = Configuration.SeaLevel - height;

			return (Configuration.SeaLevel - depth * WaterDepthFlatten);
		}

		return height;
	}

	private Color GetTerrainColor(float height)
	{
		float seaLevel = Configuration.SeaLevel;
		float maxHeight = Configuration.MaxHeight;

		if (height < seaLevel)
		{
			float depth = Mathf.InverseLerp(0f, seaLevel, height);

			return new Color(0.05f, 0.15f, 0.35f).Lerp(new Color(0.15f, 0.4f, 0.6f), depth);
		}

		float land = Mathf.InverseLerp(seaLevel, maxHeight, height);

		if (land < 0.1f) return new Color(0.85f, 0.8f, 0.6f);

		if (land < 0.45f)
		{
			return new Color(0.35f, 0.55f, 0.25f).Lerp(new Color(0.5f, 0.55f, 0.3f), (land - 0.1f) / 0.35f);
		}

		if (land < 0.75f)
		{
			return new Color(0.5f, 0.45f, 0.35f).Lerp(new Color(0.6f, 0.55f, 0.5f), (land - 0.45f) / 0.3f);
		}

		return new Color(0.9f, 0.9f, 0.92f);
	}

	#endregion
}


public partial class WorldConfiguration
{
	public readonly WorldSize Size;
	
	public readonly int ChunkCountPerWorldSide;
	public readonly int PointCountPerChunkSide = 50;
	public readonly int PointCountPerWorldSide;
	
	public readonly WorldTemplate Template;
	
	public readonly float MinHeight = 0f;
	public readonly float MaxHeight = 10_000f;
	public readonly float SeaLevel = 4_000f;
	
	public readonly uint MaxFactionCount;
	
	public WorldConfiguration(
		WorldSize size,
		WorldTemplate template
	)
	{
		Size = size;
		Template = template;
		
		switch(size)
		{
			case WorldSize.Small:
				ChunkCountPerWorldSide = 25;
				MaxFactionCount = 4;
				
				break;
			case WorldSize.Standard:
				ChunkCountPerWorldSide = 50;
				MaxFactionCount = 8;
				
				break;
			case WorldSize.Large:
				ChunkCountPerWorldSide = 75;
				MaxFactionCount = 16;
				
				break;
			case WorldSize.Huge:
				ChunkCountPerWorldSide = 100;
				MaxFactionCount = 32;
				
				break;
			default:
				ChunkCountPerWorldSide = 10;
				MaxFactionCount = 2;
				
				break;
		}
		
		PointCountPerWorldSide = ChunkCountPerWorldSide * PointCountPerChunkSide;
	}
}

public enum WorldTemplate
{
	Volcano, 
	Island,
	Continents, 
	Archipelago, 
	Atoll, 
	Mediterranean, 
	Peninsula, 
	Pangea, 
	Isthmus, 
	Shattered, 
	Taklamakan, 
	OldWorld, 
	Fractious
}

public enum WorldSize
{
	Tiny,
	Small,
	Standard,
	Large,
	Huge
}
