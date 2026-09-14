using System.Collections.Generic;
using System.Linq;

public class SessionConfiguration
{
	#region Session

	public readonly int Seed;

	public readonly Rand Rand;

	#endregion


	#region Size

	public readonly WorldSizeFeature SizeFeature;

	public readonly int ChunkCountPerWorldSide;
	public readonly int PointCountPerChunkSide = 50;
	public readonly int PointCountPerWorldSide;

	#endregion


	#region Heights

	public readonly float MinHeight = 0f;
	public readonly float MaxHeight = 10_000f;
	public readonly float SeaLevel = 4_000f;

	#endregion


	#region Players

	public readonly Player[] Players;

	public readonly int MaxFactionCount;

	#endregion


	#region Features

	public readonly WorldShapeFeature ShapeFeature;

	public readonly WorldLandformFeature[] LandformFeatures;

	public readonly WorldClimateFeature ClimateFeature;

	#endregion


	#region Construction

	public SessionConfiguration(
		int seed,
		WorldSizeFeature sizeFeature,
		WorldShapeFeature shapeFeature,
		Player[] players,
		WorldClimateFeature climateFeature = null,
		WorldLandformFeature[] landformFeatures = null
	)
	{
		Seed = seed;
		Rand = new Rand(seed);

		SizeFeature = sizeFeature;
		ChunkCountPerWorldSide = sizeFeature.ChunkCountPerWorldSide;
		MaxFactionCount = sizeFeature.MaxFactionCount;

		PointCountPerWorldSide = ChunkCountPerWorldSide * PointCountPerChunkSide;

		ShapeFeature = shapeFeature ?? new ContinentFeature();
		LandformFeatures = landformFeatures ?? System.Array.Empty<WorldLandformFeature>();
		ClimateFeature = climateFeature ?? new TemperateClimateFeature();

		Players = players ?? System.Array.Empty<Player>();
	}


	public static SessionConfiguration FromFeatures(
		int seed,
		IEnumerable<WorldFeature> features,
		IEnumerable<Player> players
	)
	{
		List<WorldFeature> list = features.ToList();

		WorldSizeFeature size = list.OfType<WorldSizeFeature>().FirstOrDefault() ?? new TinySizeFeature();

		return new SessionConfiguration(
			seed,
			size,
			list.OfType<WorldShapeFeature>().FirstOrDefault(),
			players?.ToArray(),
			list.OfType<WorldClimateFeature>().FirstOrDefault(),
			list.OfType<WorldLandformFeature>().ToArray()
		);
	}

	#endregion


	#region Access

	public Player FindPlayer(int slot)
	{
		foreach (Player player in Players)
		{
			if (player.Slot == slot)
			{
				return player;
			}
		}

		return null;
	}

	#endregion
}
