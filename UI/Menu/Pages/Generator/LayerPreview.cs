using Godot;

public enum PreviewMode
{
	Relief,
	Elevation,
	LandSea,
	Temperature,
	Moisture,
	Biome
}


public static class LayerPreview
{
	#region Constants

	private const int CHANNELS = 3;

	public const float TEMPERATURE_MIN = -60f;
	public const float TEMPERATURE_MAX = 60f;

	public static readonly PreviewMode[] MODES =
	{
		PreviewMode.Relief,
		PreviewMode.Elevation,
		PreviewMode.LandSea,
		PreviewMode.Temperature,
		PreviewMode.Moisture,
		PreviewMode.Biome
	};

	#endregion


	#region Building

	public static ImageTexture Build(
		WorldLayers layers,
		SessionConfiguration configuration,
		PreviewMode mode,
		int maxSize
	)
	{
		int side = layers.Side;
		int stride = Mathf.Max(1, side / maxSize);
		int size = side / stride;

		byte[] pixels = new byte[size * size * CHANNELS];

		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				Color color = Sample(layers, configuration, mode, x * stride, y * stride);

				int offset = (y * size + x) * CHANNELS;

				pixels[offset] = (byte)(Mathf.Clamp(color.R, 0f, 1f) * 255f);
				pixels[offset + 1] = (byte)(Mathf.Clamp(color.G, 0f, 1f) * 255f);
				pixels[offset + 2] = (byte)(Mathf.Clamp(color.B, 0f, 1f) * 255f);
			}
		}

		Image image = Image.CreateFromData(size, size, false, Image.Format.Rgb8, pixels);

		return ImageTexture.CreateFromImage(image);
	}


	private static Color Sample(
		WorldLayers layers,
		SessionConfiguration configuration,
		PreviewMode mode,
		int x,
		int y
	)
	{
		switch (mode)
		{
			case PreviewMode.Elevation:
			{
				float value = Mathf.InverseLerp(
					configuration.MinHeight,
					configuration.MaxHeight,
					layers.Height[layers.Index(x, y)]
				);

				return new Color(value, value, value);
			}

			case PreviewMode.LandSea:
			{
				bool land = layers.Height[layers.Index(x, y)] >= configuration.SeaLevel;

				return land ? new Color(0.86f, 0.84f, 0.78f) : new Color(0.10f, 0.20f, 0.34f);
			}

			case PreviewMode.Temperature:
			{
				float value = Mathf.InverseLerp(
					TEMPERATURE_MIN,
					TEMPERATURE_MAX,
					layers.Temperature[layers.Index(x, y)]
				);

				return new Color(0.15f, 0.35f, 0.85f).Lerp(new Color(0.90f, 0.30f, 0.15f), value);
			}

			case PreviewMode.Moisture:
			{
				float value = Mathf.Clamp(layers.Moisture[layers.Index(x, y)], 0f, 1f);

				return new Color(0.80f, 0.72f, 0.45f).Lerp(new Color(0.10f, 0.45f, 0.55f), value);
			}

			case PreviewMode.Biome:
			{
				int value = layers.Biome[layers.Index(x, y)];

				return Factions.Get(value % Factions.COUNT).Base;
			}

			default:
			{
				return TerrainPalette.Elevation(
					layers.Height[layers.Index(x, y)],
					configuration.MinHeight,
					configuration.SeaLevel,
					configuration.MaxHeight
				);
			}
		}
	}

	#endregion
}
