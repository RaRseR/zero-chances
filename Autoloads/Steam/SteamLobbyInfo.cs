public struct SteamLobbyInfo
{
	#region State

	public ulong Id;
	public ulong HostId;
	public string Name;
	public int MaxPlayers;
	public int PlayerCount;
	public int Ping;

	#endregion


	#region Access

	public int FreeSlots => MaxPlayers - PlayerCount;

	#endregion
}


public class SteamLobbyQuery
{
	#region State

	public string Name = string.Empty;
	public int MinTotalSlots;
	public int MinFreeSlots = 1;
	public int MaxPing;

	#endregion


	#region Filtering

	public bool Matches(SteamLobbyInfo lobby)
	{
		if (Name.Length > 0)
		{
			string title = lobby.Name ?? string.Empty;

			if (!title.ToLowerInvariant().Contains(Name.ToLowerInvariant()))
			{
				return false;
			}
		}

		if (lobby.MaxPlayers < MinTotalSlots)
		{
			return false;
		}

		if (lobby.FreeSlots < MinFreeSlots)
		{
			return false;
		}

		if (MaxPing > 0 && lobby.Ping > MaxPing)
		{
			return false;
		}

		return true;
	}

	#endregion
}
