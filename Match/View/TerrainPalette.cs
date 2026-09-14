using Godot;

public static class TerrainPalette
{
	#region Constants

	private static readonly Color DEEP_WATER = new Color(0.05f, 0.15f, 0.35f);
	private static readonly Color SHALLOW_WATER = new Color(0.15f, 0.40f, 0.60f);
	private static readonly Color SAND = new Color(0.85f, 0.80f, 0.60f);
	private static readonly Color GRASS = new Color(0.35f, 0.55f, 0.25f);
	private static readonly Color DRY_GRASS = new Color(0.50f, 0.55f, 0.30f);
	private static readonly Color ROCK = new Color(0.50f, 0.45f, 0.35f);
	private static readonly Color HIGH_ROCK = new Color(0.60f, 0.55f, 0.50f);
	private static readonly Color SNOW = new Color(0.90f, 0.90f, 0.92f);

	#endregion


	#region Access

	public static Color Elevation(float height, float minHeight, float seaLevel, float maxHeight)
	{
		if (height < seaLevel)
		{
			float depth = Mathf.InverseLerp(minHeight, seaLevel, height);

			return DEEP_WATER.Lerp(SHALLOW_WATER, depth);
		}

		float land = Mathf.InverseLerp(seaLevel, maxHeight, height);

		if (land < 0.05f)
		{
			return SAND;
		}

		if (land < 0.45f)
		{
			return GRASS.Lerp(DRY_GRASS, (land - 0.05f) / 0.40f);
		}

		if (land < 0.75f)
		{
			return ROCK.Lerp(HIGH_ROCK, (land - 0.45f) / 0.30f);
		}

		return HIGH_ROCK.Lerp(SNOW, (land - 0.75f) / 0.25f);
	}

	#endregion
}
