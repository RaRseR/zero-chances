using Godot;
using System;
using System.Collections.Generic;

public class HeightmapGenerator
{
	private int ChunkCountPerWorldSide;
	private int PointCountPerChunkSide;
	private int PointCountPerWorldSide;
	
	public WorldTemplate Template;
	
	private float MinHeight;
	private float MaxHeight;
	private float SeaLevel;
	
	private float BlobPower;
	private float LinePower;
	
	private PointArray<float> Heights;
	
	#region Steps

	private abstract record HeightmapStep;
	 
	private sealed record BlobStep(
		int RepeatCount, 
		float MinHeight, float MaxHeight, 
		float MinX, float MaxX, 
		float MinY, float MaxY
	): HeightmapStep;
	
	private sealed record LineStep(
		int RepeatCount,
		float MinHeight, float MaxHeight, 
		float MinX, float MaxX, 
		float MinY, float MaxY
	): HeightmapStep;
	 
	private sealed record StraitStep(
		int RepeatCount,
		bool IsVertical
	): HeightmapStep;
	 
	#endregion


	#region Noises

	private readonly struct NoiseLevel
	{
		public readonly float Frequency;
		public readonly int Octaves;
		public readonly float Lacunarity;
		public readonly float Gain;

		public readonly float AmplitudeFraction;

		public NoiseLevel(
			float frequency, 
			int octaves, 
			float lacunarity, 
			float gain, 
			float amplitudeFraction
		)
		{
			Frequency = frequency;
			Octaves = octaves;
			Lacunarity = lacunarity;
			Gain = gain;
			AmplitudeFraction = amplitudeFraction;
		}
	}

	private readonly struct NoiseSettings
	{
		public readonly NoiseLevel Large;
		public readonly NoiseLevel Medium;
		public readonly NoiseLevel Small;


		public NoiseSettings(
			NoiseLevel large, 
			NoiseLevel medium, 
			NoiseLevel small
		)
		{
			Large = large;
			Medium = medium;
			Small = small;
		}
	}
	 
	#endregion
	
	
	public HeightmapGenerator(
		WorldConfiguration configuration
	)
	{
		ChunkCountPerWorldSide = configuration.ChunkCountPerWorldSide;
		PointCountPerChunkSide = configuration.PointCountPerChunkSide;
		PointCountPerWorldSide = configuration.PointCountPerWorldSide;
		
		Template = configuration.Template;
		
		MinHeight = configuration.MinHeight;
		MaxHeight = configuration.MaxHeight;
		SeaLevel = configuration.SeaLevel;
		
		BlobPower = GetBlobPower(configuration.Size);
		LinePower = GetLinePower(configuration.Size);
		
		Heights = new PointArray<float>(PointCountPerWorldSide);
		
		foreach (HeightmapStep step in TemplateToSteps())
		{
			ApplyStep(step);
		}
		
		AplyNoises();
	}
	
	public PointArray<float> GetHeightmap()
	{
		return Heights;
	}
	
	private float GetBlobPower(WorldSize size)
	{
		switch (size)
		{
			case WorldSize.Tiny:
				return 0.97f;
			case WorldSize.Small:
				return 0.975f;
			case WorldSize.Standard:
				return 0.98f;
			case WorldSize.Large:
				return 0.985f;
			case WorldSize.Huge:
				return 0.99f;
			default:
				throw new ArgumentOutOfRangeException(nameof(size), size, "Unhandled WorldSize");
		}
	}

	private float GetLinePower(WorldSize size)
	{
		switch (size)
		{
			case WorldSize.Tiny:
				return 0.75f;
			case WorldSize.Small:
				return 0.775f;
			case WorldSize.Standard:
				return 0.8f;
			case WorldSize.Large:
				return 0.825f;
			case WorldSize.Huge:
				return 0.85f;
			default:
				throw new ArgumentOutOfRangeException(nameof(size), size, "Unhandled WorldSize");
		}
	}


	#region Templates

	private HeightmapStep[] TemplateToSteps()
	{
		switch(Template)
		{
			case WorldTemplate.Continents:
				return new HeightmapStep[] {
					new BlobStep(
						1, 
						0.90f, 1.00f, 
						0.65f, 0.75f, 
						0.45f, 0.55f
					),
					new BlobStep(
						Rand.RangeInt(1, 2), 
						0.25f, 0.35f, 
						0.25f, 0.30f, 
						0.20f, 0.25f
					),
					new BlobStep(
						Rand.RangeInt(1, 2), 
						0.25f, 0.35f, 
						0.75f, 0.80f, 
						0.25f, 0.75f
					),
					new BlobStep(
						Rand.RangeInt(1, 2), 
						0.25f, 0.35f, 
						0.15f, 0.25f, 
						0.55f, 0.75f
					),
					new LineStep(
						Rand.RangeInt(1, 2), 
						0.30f, 0.50f, 
						0.15f, 0.85f, 
						0.20f, 0.80f
					),
					new LineStep(
						Rand.RangeInt(2, 3), 
						-0.20f, -0.30f, 
						0.15f, 0.85f, 
						0.20f, 0.80f
					),
					new StraitStep(
						2, 
						IsVertical: true
					),
					new LineStep(
						Rand.RangeInt(1, 4), 
						0.30f, 0.40f, 
						0.15f, 0.85f, 
						0.20f, 0.80f
					),
					new BlobStep(
						Rand.RangeInt(3, 4), 
						-0.15f, -0.25f, 
						0.15f, 0.85f, 
						0.20f, 0.80f
					)
				};
			case WorldTemplate.Archipelago:
				return new HeightmapStep[] {
					new LineStep(
						Rand.RangeInt(2, 3), 
						0.40f, 0.60f, 
						0.20f, 0.80f,
						0.20f, 0.80f
					),
					new BlobStep(
						5, 
						0.15f, 0.20f, 
						0.10f, 0.90f, 
						0.30f, 0.70f
					),
					new BlobStep(
						2, 
						0.10f, 0.15f, 
						0.10f, 0.30f, 
						0.20f, 0.80f
					),
					new BlobStep(
						2, 
						0.10f, 0.15f, 
						0.60f, 0.90f, 
						0.20f, 0.80f
					),
					new LineStep(
						Rand.RangeInt(10, 15), 
						-0.20f, -0.30f, 
						0.05f, 0.95f, 
						0.05f, 0.95f
					),
					new StraitStep(
						Rand.RangeInt(2, 5), 
						IsVertical: true
					),
					new StraitStep(
						Rand.RangeInt(2, 5), 
						IsVertical: false
					)
				};
			case WorldTemplate.Pangea:
				return new HeightmapStep[] {
					new BlobStep(
						Rand.RangeInt(1, 2), 
						0.25f, 0.40f, 
						0.15f, 0.50f, 
						0.00f, 0.10f
					),
					new BlobStep(
						Rand.RangeInt(1, 2), 
						0.05f, 0.40f, 
						0.50f, 0.85f, 
						0.00f, 0.10f
					),
					new BlobStep(
						Rand.RangeInt(1, 2), 
						0.25f, 0.40f, 
						0.50f, 0.85f, 
						0.90f, 1.00f
					),
					new BlobStep(
						Rand.RangeInt(1, 2), 
						0.05f, 0.40f, 
						0.15f, 0.50f, 
						0.90f, 1.00f
					),
					new BlobStep(
						Rand.RangeInt(8, 12),
						0.20f, 0.40f, 
						0.20f, 0.80f, 
						0.48f, 0.52f
					),
					new LineStep(
						Rand.RangeInt(2, 3), 
						-0.20f, -0.30f, 
						0.05f, 0.95f, 
						0.10f, 0.20f
					),
					new LineStep(
						Rand.RangeInt(2, 3), 
						-0.20f, -0.30f, 
						0.05f, 0.95f, 
						0.80f, 0.90f
					),
					new LineStep(
						Rand.RangeInt(3, 4), 
						0.40f, 0.60f, 
						0.10f, 0.90f, 
						0.20f, 0.80f
					)
				};
			case WorldTemplate.Island:
				return new HeightmapStep[] {
					new LineStep(
						1, 
						0.80f, 0.90f, 
						0.40f, 0.60f, 
						0.40f, 0.60f
					),
					new BlobStep(
						Rand.RangeInt(5, 6), 
						0.35f, 0.45f, 
						0.25f, 0.55f, 
						0.45f, 0.55f
					),
					new BlobStep(
						1, 
						0.40f, 0.50f, 
						0.45f, 0.65f, 
						0.35f, 0.65f
					),
					new BlobStep(
						Rand.RangeInt(2, 3), 
						0.35f, 0.45f, 
						0.20f, 0.80f, 
						0.20f, 0.80f
					),
					new LineStep(
						Rand.RangeInt(4, 6), 
						-0.15f, -0.20f, 
						0.20f, 0.80f, 
						0.20f, 0.80f
					),
					new BlobStep(
						Rand.RangeInt(3, 4), 
						-0.10f, -0.15f, 
						0.15f, 0.80f, 
						0.15f, 0.80f
					)
				};
			default:
				throw new ArgumentOutOfRangeException(nameof(Template), Template, "Unhandled WorldTemplate");
		}
	}

	#endregion
	
	
	#region Step Dispatch
	
	private void ApplyStep(HeightmapStep step)
	{
		switch (step)
		{
			case BlobStep blobStep:
				AddBlob(blobStep);
				break;
			case LineStep lineStep:
				AddLine(lineStep);
				break;
			case StraitStep straitStep:
				AddStrait(straitStep);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(step), step, "Unhandled Step");
		}
	}
	
	#endregion
	
	
	#region Blob Tools
	
	private void AddBlob(BlobStep step)
	{
		int repeatCount = step.RepeatCount;
 
		for (int r = 0; r < repeatCount; r++)
		{
			PointArray<float> change = new(PointCountPerWorldSide);
 			PointArray<bool> visited = new(PointCountPerWorldSide);
			
			float height = ResolveHeight(step.MinHeight, step.MaxHeight);
			
			int startX = ResolveIndex(step.MinX, step.MaxX);
	 		int startY = ResolveIndex(step.MinY, step.MaxY);
			
			change[startX, startY] = height;
			visited[startX, startY] = true;
			
			Queue<(int x, int y)> queue = new();
			queue.Enqueue((startX, startY));
	 
			while (queue.Count > 0)
			{
				(int x, int y) = queue.Dequeue();
				
				for (int di = 0; di < PointDirections.Count; di++)
				{
					int nx = x + PointDirections.OffsetX[di];
					int ny = y + PointDirections.OffsetY[di];
 
					if (nx < 0 || ny < 0 || nx >= PointCountPerWorldSide || ny >= PointCountPerWorldSide)
					{
						continue;
					}
					
					float blobedHeight = MathF.Pow(change[x, y], BlobPower);
 
					change[nx, ny] = blobedHeight;
					visited[nx, ny] = true;
 
					for (int dj = 0; dj < PointDirections.Count; dj++)
					{
						int nnx = nx + PointDirections.OffsetX[dj];
						int nny = ny + PointDirections.OffsetY[dj];
						
						if (visited[nnx, nny])
						{
							continue;
						}
						
						queue.Enqueue((nnx, nny));
					}
				}
			}
	 
			for (int y = 0; y < PointCountPerWorldSide; y++)
			{
				for (int x = 0; x < PointCountPerWorldSide; x++)
				{
					if (!visited[x, y]) continue;
 
					Heights[x, y] = LimitHeight(Heights[x, y] + change[x, y]);
				}
			}
		}
	}
	
	#endregion
	
	
	#region Line Tools
	
	private void AddLine(LineStep step)
	{
		int repeatCount = step.RepeatCount;
 
		int minDistance = PointCountPerWorldSide / 8;
		int maxDistance = PointCountPerWorldSide / 2;

		for (int r = 0; r < repeatCount; r++)
		{
			PointArray<float> change = new(PointCountPerWorldSide);
			PointArray<bool> visited = new(PointCountPerWorldSide);
			
			int startX = ResolveIndex(step.MinX, step.MaxX);
	 		int startY = ResolveIndex(step.MinY, step.MaxY);
			
			int endX = ResolveIndex(step.MinX, step.MaxX);
	 		int endY = ResolveIndex(step.MinY, step.MaxY);
			
			(int x, int y)[] path = Heights.TracePath(
				startX, startY, 
				endX, endY
			);
			
			foreach ((int x, int y) in path)
			{
				float height = ResolveHeight(step.MinHeight, step.MaxHeight);
				
				change[x, y] = height;
				visited[x, y] = true;
				
				//for (int d = 0; d < PointDirections.Count; d++)
				//{
					//int nx = x + PointDirections.OffsetX[d];
					//int ny = y + PointDirections.OffsetY[d];
				//}
			}
			
			for (int y = 0; y < PointCountPerWorldSide; y++)
			{
				for (int x = 0; x < PointCountPerWorldSide; x++)
				{
					if (!visited[x, y]) continue;

					Heights[x, y] = LimitHeight(Heights[x, y] + change[x, y]);
				}
			}
		}
	}
	
	private void AddProminences(
		float[] heights, 
		uint[][] neighbours, 
		List<int> ridge, 
		int ringCount
	)
	{
		const int PROMINENCE_INTERVAL = 6;
 
		for (int d = 0; d < ridge.Count; d++)
		{
			if (d % PROMINENCE_INTERVAL != 0) continue;
 
			int current = ridge[d];
 
			for (int step = 0; step < ringCount; step++)
			{
				int lowest = -1;
				float lowestHeight = float.PositiveInfinity;
 
				foreach (int neighbour in neighbours[current])
				{
					if (heights[neighbour] < lowestHeight)
					{
						lowestHeight = heights[neighbour];
						lowest = neighbour;
					}
				}
 
				if (lowest == -1) break;
 
				heights[lowest] = (heights[current] * 2f + heights[lowest]) / 3f;
				current = lowest;
			}
		}
	}
	
	private void AddStrait(StraitStep step)
	{
		//uint width = Math.Min((uint)step.Width.Resolve(), siteCountX / 3u);
 //
		//if (width < 1) return;
 //
		//bool[] used = new bool[heights.Length];
 //
		//const float EDGE_MARGIN = 5f;
 //
		//float startX = step.IsVertical
			//? Rand.NextSingle() * graphWidth * 0.4f + graphWidth * 0.3f
			//: EDGE_MARGIN;
 //
		//float startY = step.IsVertical
			//? EDGE_MARGIN
			//: Rand.NextSingle() * graphHeight * 0.4f + graphHeight * 0.3f;
 //
		//float endX = step.IsVertical
			//? graphWidth - startX - graphWidth * 0.1f + Rand.NextSingle() * graphWidth * 0.2f
			//: graphWidth - EDGE_MARGIN;
 //
		//float endY = step.IsVertical
			//? graphHeight - EDGE_MARGIN
			//: graphHeight - startY - graphHeight * 0.1f + Rand.NextSingle() * graphHeight * 0.2f;
 //
		//int start = FindSite(spacing, siteCountX, siteCountY, startX, startY);
		//int end = FindSite(spacing, siteCountX, siteCountY, endX, endY);
 //
		//List<int> path = TracePath(positions, neighbours, start, end, new bool[heights.Length], 0.8f);
 //
		//float exponent = 0.9f - 0.1f;
 //
		//List<int> current = new(path);
 //
		//for (int i = 0; i < width; i++)
		//{
			//List<int> next = new();
 //
			//foreach (int site in current)
			//{
				//foreach (int neighbour in neighbours[site])
				//{
					//if (used[neighbour]) continue;
 //
					//used[neighbour] = true;
					//next.Add(neighbour);
 //
					//heights[neighbour] = MathF.Pow(heights[neighbour], exponent);
 //
					//if (heights[neighbour] > WorldHeight.MaxHeight) heights[neighbour] = 5f;
				//}
			//}
 //
			//current = next;
		//}
	}
	
	#endregion
	
	
	private float ResolveHeight(float min, float max)
	{
		float sign = (min < 0f || max < 0f) ? -1f : 1f;
		
		float absMin = Mathf.Abs(min);
		float absMax = Mathf.Abs(max);
		
		float realMin = Mathf.Min(absMin, absMax);
		float realMax = Mathf.Max(absMin, absMax);
		
		float magnitude = MinHeight + (MaxHeight - MinHeight) * Rand.RangeFloat(realMin, realMax);

		return sign * magnitude;
	}
	
	private int ResolveIndex(float min, float max)
	{
		return Mathf.Clamp((int)((PointCountPerWorldSide - 1) * Rand.RangeFloat(min, max)), 0, PointCountPerWorldSide - 1);
	}
	
	
	#region Noises
	
	private void AplyNoises()
	{
		NoiseSettings settings = GetNoiseSettings();
		
		FastNoiseLite largeNoise = BuildNoise(settings.Large);
		FastNoiseLite mediumNoise = BuildNoise(settings.Medium);
		FastNoiseLite smallNoise = BuildNoise(settings.Small);
		
		float largeAmplitude = MaxHeight * settings.Large.AmplitudeFraction;
		float mediumAmplitude = MaxHeight * settings.Medium.AmplitudeFraction;
		float smallAmplitude = MaxHeight * settings.Small.AmplitudeFraction;
		
		for (ushort y = 0; y < PointCountPerWorldSide; y++)
		{
			for (ushort x = 0; x < PointCountPerWorldSide; x++)
			{
				float sample = largeNoise.GetNoise2D(x, y) * largeAmplitude
					+ mediumNoise.GetNoise2D(x, y) * mediumAmplitude
					+ smallNoise.GetNoise2D(x, y) * smallAmplitude;

				Heights[x, y] = LimitHeight(Heights[x, y] + sample);
			}
		}
	}
	
	private NoiseSettings GetNoiseSettings()
	{
		switch (Template)
		{
			case WorldTemplate.Continents:
				return new NoiseSettings(
					large: new NoiseLevel(
						frequency: 0.003f, 
						octaves: 2, 
						lacunarity: 2.0f, 
						gain: 0.5f, 
						amplitudeFraction: 0.05f
					),
					medium: new NoiseLevel(
						frequency: 0.01f,  
						octaves: 3, 
						lacunarity: 2.0f, 
						gain: 0.5f, 
						amplitudeFraction: 0.06f
					),
					small: new NoiseLevel(
						frequency: 0.04f,  
						octaves: 3, 
						lacunarity: 2.0f, 
						gain: 0.5f, 
						amplitudeFraction: 0.03f
					)
				);
			case WorldTemplate.Archipelago:
				return new NoiseSettings(
					large: new NoiseLevel(
						frequency: 0.005f, 
						octaves: 2, 
						lacunarity: 2.0f, 
						gain: 0.5f, 
						amplitudeFraction: 0.08f
					),
					medium: new NoiseLevel(
						frequency: 0.02f,  
						octaves: 4, 
						lacunarity: 2.2f, 
						gain: 0.55f, 
						amplitudeFraction: 0.10f
					),
					small: new NoiseLevel(
						frequency: 0.06f,  
						octaves: 4, 
						lacunarity: 2.2f, 
						gain: 0.55f, 
						amplitudeFraction: 0.06f
					)
				);
			case WorldTemplate.Pangea:
				return new NoiseSettings(
					large: new NoiseLevel(
						frequency: 0.002f, 
						octaves: 2, 
						lacunarity: 2.0f, 
						gain: 0.45f, 
						amplitudeFraction: 0.03f
					),
					medium: new NoiseLevel(
						frequency: 0.008f, 
						octaves: 3, 
						lacunarity: 2.0f, 
						gain: 0.45f, 
						amplitudeFraction: 0.03f
					),
					small: new NoiseLevel(
						frequency: 0.03f,  
						octaves: 2, 
						lacunarity: 2.0f, 
						gain: 0.45f, 
						amplitudeFraction: 0.02f
					)
				);
			case WorldTemplate.Island:
				return new NoiseSettings(
					large: new NoiseLevel(
						frequency: 0.004f, 
						octaves: 2, 
						lacunarity: 2.0f, 
						gain: 0.5f, 
						amplitudeFraction: 0.06f
					),
					medium: new NoiseLevel(
						frequency: 0.015f, 
						octaves: 3, 
						lacunarity: 2.0f, 
						gain: 0.5f, 
						amplitudeFraction: 0.06f
					),
					small: new NoiseLevel(
						frequency: 0.05f,  
						octaves: 3, 
						lacunarity: 2.0f, 
						gain: 0.5f, 
						amplitudeFraction: 0.04f
					)
				);
			default:
				throw new ArgumentOutOfRangeException(nameof(Template), Template, "Unhandled WorldTemplate");
		}
	}
	
	private FastNoiseLite BuildNoise(NoiseLevel level)
	{
		var noise = new FastNoiseLite();
		noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin;
		noise.Seed = Rand.NextInt();
		noise.Frequency = level.Frequency;
		noise.FractalType = FastNoiseLite.FractalTypeEnum.Fbm;
		noise.FractalOctaves = level.Octaves;
		noise.FractalLacunarity = level.Lacunarity;
		noise.FractalGain = level.Gain;

		return noise;
	}
	
	#endregion
	
	
	#region Helpers
	
	private float LimitHeight(float height)
	{
		return Mathf.Clamp(height, MinHeight, MaxHeight);
	}
	
	#endregion
	
}
