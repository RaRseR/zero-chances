using System.Collections.Generic;

public static class RosterCodec
{
	#region Constants

	public const int MAX_SLOTS = 16;

	private const byte FLAG_HOST = 1;
	private const byte FLAG_BOT = 2;
	private const byte FLAG_WORLD = 4;

	#endregion


	#region Writing

	public static byte[] Pack(IReadOnlyList<Player> players, string name, int maxPlayers)
	{
		MessageWriter writer = new MessageWriter();

		writer.Text(name);
		writer.Var(maxPlayers);
		writer.Var(players.Count);

		foreach (Player player in players)
		{
			byte flags = 0;

			if (player.IsHost)
			{
				flags |= FLAG_HOST;
			}

			if (player.IsBot)
			{
				flags |= FLAG_BOT;
			}

			if (player.CanEditWorld)
			{
				flags |= FLAG_WORLD;
			}

			writer.Var(player.Slot);
			writer.Var(player.PeerId);
			writer.ULong(player.SteamId);
			writer.Text(player.Name);
			writer.Var(player.FactionIndex);
			writer.Byte(flags);
		}

		return writer.ToArray();
	}

	#endregion


	#region Reading

	public static bool Unpack(byte[] data, List<Player> target, out string name, out int maxPlayers)
	{
		MessageReader reader = new MessageReader(data);

		name = reader.Text();
		maxPlayers = reader.Var();

		int count = reader.Var();

		if (reader.Failed || count < 0 || count > MAX_SLOTS)
		{
			return false;
		}

		List<Player> parsed = new();

		for (int index = 0; index < count; index++)
		{
			Player player = new Player
			{
				Slot = reader.Var(),
				PeerId = reader.Var(),
				SteamId = reader.ULong(),
				Name = reader.Text(),
				FactionIndex = reader.Var()
			};

			byte flags = reader.Byte();

			player.IsHost = (flags & FLAG_HOST) != 0;
			player.IsBot = (flags & FLAG_BOT) != 0;
			player.CanEditWorld = (flags & FLAG_WORLD) != 0;

			if (reader.Failed)
			{
				return false;
			}

			parsed.Add(player);
		}

		target.Clear();
		target.AddRange(parsed);

		return true;
	}

	#endregion
}
