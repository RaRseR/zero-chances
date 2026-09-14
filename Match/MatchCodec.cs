using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

[Flags]
public enum WorldLayerMask : byte
{
	None = 0,
	Height = 1 << 0,
	Temperature = 1 << 1,
	Moisture = 1 << 2,
	Biome = 1 << 3,
	RockType = 1 << 4,
	Resources = 1 << 5
}


public static class MatchCodec
{
	#region Constants

	private const byte VERSION = 1;

	private const int QUANTIZATION_STEPS = 65535;

	private const WorldLayerMask SENT_LAYERS = WorldLayerMask.Height;

	#endregion


	#region Configuration

	public static byte[] EncodeConfiguration(SessionConfiguration configuration)
	{
		MessageWriter writer = new MessageWriter();

		writer.Byte(VERSION);
		writer.Int(configuration.Seed);
		writer.Text(configuration.SizeFeature.GetType().Name);

		writer.Text(configuration.ShapeFeature.GetType().Name);
		writer.Text(configuration.ClimateFeature.GetType().Name);

		writer.Var(configuration.LandformFeatures.Length);

		foreach (WorldLandformFeature landform in configuration.LandformFeatures)
		{
			writer.Text(landform.GetType().Name);
		}

		writer.Blob(RosterCodec.Pack(configuration.Players, string.Empty, configuration.Players.Length));

		return writer.ToArray();
	}


	public static SessionConfiguration DecodeConfiguration(byte[] data)
	{
		MessageReader reader = new MessageReader(data);

		if (reader.Byte() != VERSION)
		{
			return null;
		}

		int seed = reader.Int();

		WorldSizeFeature size = FeatureRules.Create(reader.Text()) as WorldSizeFeature;

		WorldShapeFeature shape = FeatureRules.Create(reader.Text()) as WorldShapeFeature;

		WorldClimateFeature climate = FeatureRules.Create(reader.Text()) as WorldClimateFeature;

		int landformCount = reader.Var();

		if (reader.Failed || landformCount < 0 || landformCount > 64)
		{
			return null;
		}

		List<WorldLandformFeature> landforms = new();

		for (int index = 0; index < landformCount; index++)
		{
			if (FeatureRules.Create(reader.Text()) is WorldLandformFeature landform)
			{
				landforms.Add(landform);
			}
		}

		List<Player> players = new();

		RosterCodec.Unpack(reader.Blob(), players, out _, out _);

		if (reader.Failed || size == null)
		{
			return null;
		}

		return new SessionConfiguration(
			seed, size, shape, players.ToArray(), climate, landforms.ToArray()
		);
	}

	#endregion


	#region World

	public static byte[] EncodeWorld(WorldLayers layers, SessionConfiguration configuration)
	{
		MessageWriter writer = new MessageWriter(layers.PointCount);

		writer.Byte(VERSION);
		writer.Var(layers.Side);
		writer.Byte((byte)SENT_LAYERS);

		WriteHeight(writer, layers, configuration);

		return Compress(writer.ToArray());
	}


	public static WorldLayers DecodeWorld(byte[] data, SessionConfiguration configuration)
	{
		MessageReader reader = new MessageReader(Decompress(data));

		if (reader.Byte() != VERSION)
		{
			return null;
		}

		int side = reader.Var();

		WorldLayerMask mask = (WorldLayerMask)reader.Byte();

		if (reader.Failed || side <= 0 || side != configuration.PointCountPerWorldSide)
		{
			return null;
		}

		WorldLayers layers = new WorldLayers(side);

		if ((mask & WorldLayerMask.Height) != 0)
		{
			ReadHeight(reader, layers, configuration);
		}

		return reader.Failed ? null : layers;
	}


	private static void WriteHeight(MessageWriter writer, WorldLayers layers, SessionConfiguration configuration)
	{
		float[] height = layers.Height;

		float minHeight = configuration.MinHeight;
		float range = Math.Max(configuration.MaxHeight - minHeight, 1f);

		int side = layers.Side;

		for (int y = 0; y < side; y++)
		{
			int row = y * side;
			int previous = 0;

			for (int x = 0; x < side; x++)
			{
				int quantized = Quantize(height[row + x], minHeight, range);

				writer.Var(quantized - previous);

				previous = quantized;
			}
		}
	}


	private static void ReadHeight(MessageReader reader, WorldLayers layers, SessionConfiguration configuration)
	{
		float[] height = layers.Height;

		float minHeight = configuration.MinHeight;
		float range = Math.Max(configuration.MaxHeight - minHeight, 1f);

		int side = layers.Side;

		for (int y = 0; y < side; y++)
		{
			int row = y * side;
			int previous = 0;

			for (int x = 0; x < side; x++)
			{
				previous += reader.Var();

				if (reader.Failed)
				{
					return;
				}

				height[row + x] = minHeight + Math.Clamp(previous, 0, QUANTIZATION_STEPS) / (float)QUANTIZATION_STEPS * range;
			}
		}
	}


	private static int Quantize(float value, float minHeight, float range)
	{
		return Math.Clamp((int)((value - minHeight) / range * QUANTIZATION_STEPS), 0, QUANTIZATION_STEPS);
	}

	#endregion


	#region Compression

	public static byte[] Compress(byte[] data)
	{
		using MemoryStream output = new MemoryStream();

		using (DeflateStream stream = new DeflateStream(output, CompressionLevel.Fastest, true))
		{
			stream.Write(data, 0, data.Length);
		}

		return output.ToArray();
	}


	public static byte[] Decompress(byte[] data)
	{
		using MemoryStream input = new MemoryStream(data);
		using DeflateStream stream = new DeflateStream(input, CompressionMode.Decompress);
		using MemoryStream output = new MemoryStream();

		stream.CopyTo(output);

		return output.ToArray();
	}

	#endregion
}
