using Godot;

public partial class World : Node3D
{
	#region Constants

	public const float DISTANCE_BETWEEN_POINTS = 100f;

	private const float TEXTURE_CELLS_PER_TILE = 32f;
	private const float MIN_TEXELS_PER_QUAD = 4f;

	private static readonly int[] LOD_STRIDES = { 1, 2, 5, 10, 25 };

	private const int WATER_SUBDIVISIONS = 255;

	#endregion


	#region State

	private SessionConfiguration Configuration;

	private float[] Heights;
	private int Side;

	#endregion


	#region Nodes

	private Node3D TerrainRootInstance;
	private Material TerrainMaterial;
	private WorldBody[] TerrainChunks;

	private Material WaterMaterial;
	private MeshInstance3D WaterInstance;

	private Camera CameraInstance;

	private int CurrentLod = -1;

	#endregion


	#region Callbacks

	public void Build(WorldLayers layers, SessionConfiguration configuration, Camera camera)
	{
		Configuration = configuration;
		CameraInstance = camera;

		Heights = layers.Height;
		Side = layers.Side;

		TerrainMaterial = CreateTerrainMaterial();
		WaterMaterial = CreateWaterMaterial();

		BuildTerrain();
		BuildWater();

		SetupCamera();
	}


	public override void _ExitTree()
	{
		if (CameraInstance != null)
		{
			CameraInstance.ZoomChanged -= ApplyLod;
		}
	}

	#endregion


	#region Materials

	private static Material CreateTerrainMaterial()
	{
		StandardMaterial3D material = new StandardMaterial3D();

		material.VertexColorUseAsAlbedo = true;
		material.Roughness = 1f;
		material.SpecularMode = BaseMaterial3D.SpecularModeEnum.Disabled;

		return material;
	}


	private static Material CreateWaterMaterial()
	{
		StandardMaterial3D material = new StandardMaterial3D();

		material.AlbedoColor = new Color(0.12f, 0.32f, 0.55f, 0.72f);
		material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
		material.Roughness = 0.15f;

		return material;
	}

	#endregion


	#region Building

	private void BuildTerrain()
	{
		TerrainRootInstance = new();
		AddChild(TerrainRootInstance);

		int chunkCount = Configuration.ChunkCountPerWorldSide;
		int pointsPerChunk = Configuration.PointCountPerChunkSide;

		TerrainChunks = new WorldBody[chunkCount * chunkCount];

		for (int chunkY = 0; chunkY < chunkCount; chunkY++)
		{
			for (int chunkX = 0; chunkX < chunkCount; chunkX++)
			{
				WorldBody body = new WorldBody(
					BuildLods(chunkX, chunkY),
					TerrainMaterial,
					CollisionMode.None
				);

				body.Position = new Vector3(
					chunkX * pointsPerChunk * DISTANCE_BETWEEN_POINTS,
					0f,
					chunkY * pointsPerChunk * DISTANCE_BETWEEN_POINTS
				);

				TerrainRootInstance.AddChild(body);

				TerrainChunks[chunkY * chunkCount + chunkX] = body;
			}
		}
	}


	private void BuildWater()
	{
		float worldSide = Configuration.PointCountPerWorldSide * DISTANCE_BETWEEN_POINTS;

		PlaneMesh plane = new PlaneMesh();

		plane.Size = new Vector2(worldSide * 2f, worldSide * 2f);
		plane.SubdivideWidth = WATER_SUBDIVISIONS;
		plane.SubdivideDepth = WATER_SUBDIVISIONS;

		WaterInstance = new MeshInstance3D();

		WaterInstance.Name = "Water";
		WaterInstance.Mesh = plane;
		WaterInstance.MaterialOverride = WaterMaterial;
		WaterInstance.Position = new Vector3(worldSide / 2f, Configuration.SeaLevel, worldSide / 2f);

		AddChild(WaterInstance);
	}


	private ArrayMesh[] BuildLods(int chunkX, int chunkY)
	{
		int pointsPerChunk = Configuration.PointCountPerChunkSide;
		int worldSide = Configuration.PointCountPerWorldSide;

		int originX = chunkX * pointsPerChunk;
		int originY = chunkY * pointsPerChunk;

		bool hasNeighbourX = originX + pointsPerChunk < worldSide;
		bool hasNeighbourY = originY + pointsPerChunk < worldSide;

		ArrayMesh[] lods = new ArrayMesh[LOD_STRIDES.Length];

		for (int i = 0; i < LOD_STRIDES.Length; i++)
		{
			int stride = LOD_STRIDES[i];
			int stepsPerChunk = pointsPerChunk / stride;

			int sampleCountX = stepsPerChunk + (hasNeighbourX ? 1 : 0);
			int sampleCountY = stepsPerChunk + (hasNeighbourY ? 1 : 0);

			int vertexCount = sampleCountX * sampleCountY;

			Vector3[] vertices = new Vector3[vertexCount];
			Vector3[] normals = new Vector3[vertexCount];
			Color[] colors = new Color[vertexCount];
			Vector2[] uvs = new Vector2[vertexCount];

			for (int localY = 0; localY < sampleCountY; localY++)
			{
				for (int localX = 0; localX < sampleCountX; localX++)
				{
					int offsetX = localX * stride;
					int offsetY = localY * stride;

					int globalX = originX + offsetX;
					int globalY = originY + offsetY;

					float height = Heights[globalY * Side + globalX];
					int index = localY * sampleCountX + localX;

					vertices[index] = new Vector3(offsetX * DISTANCE_BETWEEN_POINTS, height, offsetY * DISTANCE_BETWEEN_POINTS);
					normals[index] = ComputeNormal(globalX, globalY, stride);
					colors[index] = TerrainPalette.Elevation(
						height,
						Configuration.MinHeight,
						Configuration.SeaLevel,
						Configuration.MaxHeight
					);
					uvs[index] = new Vector2(globalX, globalY) / TEXTURE_CELLS_PER_TILE;
				}
			}

			int quadCountX = sampleCountX - 1;
			int quadCountY = sampleCountY - 1;

			int[] indices = new int[quadCountX * quadCountY * 6];
			int cursor = 0;

			for (int quadY = 0; quadY < quadCountY; quadY++)
			{
				for (int quadX = 0; quadX < quadCountX; quadX++)
				{
					int i00 = quadY * sampleCountX + quadX;
					int i10 = i00 + 1;
					int i01 = i00 + sampleCountX;
					int i11 = i01 + 1;

					indices[cursor++] = i00;
					indices[cursor++] = i10;
					indices[cursor++] = i11;

					indices[cursor++] = i00;
					indices[cursor++] = i11;
					indices[cursor++] = i01;
				}
			}

			Godot.Collections.Array arrays = new Godot.Collections.Array();

			arrays.Resize((int)Mesh.ArrayType.Max);

			arrays[(int)Mesh.ArrayType.Vertex] = vertices;
			arrays[(int)Mesh.ArrayType.Normal] = normals;
			arrays[(int)Mesh.ArrayType.Color] = colors;
			arrays[(int)Mesh.ArrayType.TexUV] = uvs;
			arrays[(int)Mesh.ArrayType.Index] = indices;

			ArrayMesh mesh = new ArrayMesh();

			mesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, arrays);

			lods[i] = mesh;
		}

		return lods;
	}


	private Vector3 ComputeNormal(int globalX, int globalY, int stride)
	{
		int worldSide = Configuration.PointCountPerWorldSide;

		int leftX = Mathf.Max(0, globalX - stride);
		int rightX = Mathf.Min(worldSide - 1, globalX + stride);
		int downY = Mathf.Max(0, globalY - stride);
		int upY = Mathf.Min(worldSide - 1, globalY + stride);

		float heightLeft = Heights[globalY * Side + leftX];
		float heightRight = Heights[globalY * Side + rightX];
		float heightDown = Heights[downY * Side + globalX];
		float heightUp = Heights[upY * Side + globalX];

		float spanX = Mathf.Max((rightX - leftX) * DISTANCE_BETWEEN_POINTS, DISTANCE_BETWEEN_POINTS);
		float spanY = Mathf.Max((upY - downY) * DISTANCE_BETWEEN_POINTS, DISTANCE_BETWEEN_POINTS);

		Vector3 normal = new Vector3(
			(heightLeft - heightRight) / spanX,
			1f,
			(heightDown - heightUp) / spanY
		);

		return normal.Normalized();
	}

	#endregion


	#region Camera

	private void SetupCamera()
	{
		if (CameraInstance == null)
		{
			return;
		}

		float worldSide = Configuration.PointCountPerWorldSide * DISTANCE_BETWEEN_POINTS;
		Vector3 worldCenter = new(worldSide / 2f, 0f, worldSide / 2f);

		CameraInstance.SetupCamera(worldCenter, worldSide);
		CameraInstance.ZoomChanged += ApplyLod;

		ApplyLod(CameraInstance.TargetPxSize);
	}

	#endregion


	#region Terrain LOD

	private void ApplyLod(float pxSize)
	{
		int lod = SelectLod(pxSize);

		if (lod == CurrentLod) return;

		CurrentLod = lod;

		foreach (WorldBody chunk in TerrainChunks)
		{
			chunk.SetLod(lod);
		}
	}


	private int SelectLod(float pxSize)
	{
		for (int i = 0; i < LOD_STRIDES.Length; i++)
		{
			if (DISTANCE_BETWEEN_POINTS * LOD_STRIDES[i] / pxSize >= MIN_TEXELS_PER_QUAD)
			{
				return i;
			}
		}

		return LOD_STRIDES.Length - 1;
	}

	#endregion
}
