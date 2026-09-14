using Godot;
using System;
using System.Collections.Generic;

public partial class SessionManager : Node
{
	#region Constants

	private const float KICK_DELAY = 0.5f;

	#endregion


	#region Events

	public static event Action RosterChanged;

	#endregion


	#region State

	public static SessionManager Instance { get; private set; }

	public string SessionName { get; private set; } = string.Empty;
	public string Password { get; private set; } = string.Empty;
	public int MaxPlayers { get; private set; }

	public IReadOnlyList<Player> Players => Roster;

	private readonly List<Player> Roster = new();
	private readonly Dictionary<int, int> FactionBySlot = new();
	private readonly HashSet<int> Editors = new();
	private readonly HashSet<ulong> Banned = new();

	private Rand FactionRand;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		Instance = this;

		FactionRand = new Rand(System.Environment.TickCount);
		SessionName = $"{NetworkManager.LocalName}'s Lobby";

		NetworkManager.PeerJoined += OnPeerJoined;
		NetworkManager.PeerLeft += OnPeerLeft;
		NetworkManager.Closed += OnNetworkClosed;
	}


	public override void _ExitTree()
	{
		NetworkManager.PeerJoined -= OnPeerJoined;
		NetworkManager.PeerLeft -= OnPeerLeft;
		NetworkManager.Closed -= OnNetworkClosed;

		Instance = null;
	}

	#endregion


	#region Lifecycle

	public bool BeginHost(string name, int maxPlayers, string password)
	{
		Reset();

		SessionName = string.IsNullOrWhiteSpace(name) ? $"{NetworkManager.LocalName}'s Lobby" : name;
		MaxPlayers = Mathf.Clamp(maxPlayers, 1, RosterCodec.MAX_SLOTS);
		Password = password ?? string.Empty;

		NetworkManager.Instance.Admit = Admit;

		if (!NetworkManager.Instance.Host(Password))
		{
			return false;
		}

		AddPlayer(FreeSlot(), NetworkManager.LocalPeerId, NetworkManager.LocalIdentity, NetworkManager.LocalName, true);

		SteamManager.Instance?.CreateLobby(SessionName, MaxPlayers);

		GD.Print($"[Session] hosting \"{SessionName}\" for {MaxPlayers}");

		Rebuild();

		return true;
	}


	public bool BeginJoin(SteamLobbyInfo lobby, string password)
	{
		Reset();

		RosterChanged?.Invoke();

		SteamManager.Instance?.JoinLobby(lobby.Id);

		return NetworkManager.Instance.Join(lobby.HostId, password);
	}


	public void Leave()
	{
		if (NetworkManager.IsHost)
		{
			SteamManager.Instance?.CloseLobby();
		}

		bool wasActive = NetworkManager.IsActive;

		NetworkManager.Instance?.Close();

		SteamManager.Instance?.LeaveLobby();

		if (!wasActive)
		{
			Reset();

			RosterChanged?.Invoke();
		}
	}


	public void UpdateSettings(string name, int maxPlayers, string password)
	{
		if (!NetworkManager.IsHost)
		{
			return;
		}

		SessionName = string.IsNullOrWhiteSpace(name) ? SessionName : name;
		MaxPlayers = Mathf.Clamp(maxPlayers, 1, RosterCodec.MAX_SLOTS);
		Password = password ?? string.Empty;

		NetworkManager.Instance.SetPassword(Password);

		TrimBots();

		SteamManager.Instance?.PublishLobby(SessionName, MaxPlayers);

		Rebuild();
	}


	private void Reset()
	{
		Roster.Clear();
		FactionBySlot.Clear();
		Editors.Clear();
		Banned.Clear();
	}


	private void OnNetworkClosed()
	{
		Reset();

		RosterChanged?.Invoke();
	}

	#endregion


	#region Admission

	private string Admit(ulong identity, string name)
	{
		if (Banned.Contains(identity))
		{
			return "You were kicked from this lobby.";
		}

		if (Roster.Count >= MaxPlayers && !RemoveOneBot())
		{
			return "The lobby is full.";
		}

		return null;
	}


	private void OnPeerJoined(int peerId)
	{
		if (!NetworkManager.IsHost)
		{
			return;
		}

		int slot = FreeSlot();

		if (slot < 0)
		{
			NetworkManager.Instance.Disconnect(peerId);
			return;
		}

		AddPlayer(slot, peerId, NetworkManager.Instance.IdentityOf(peerId), NetworkManager.Instance.NameOf(peerId), false);

		GD.Print($"[Session] {NetworkManager.Instance.NameOf(peerId)} took slot {slot}");

		Rebuild();
	}


	private void OnPeerLeft(int peerId)
	{
		if (!NetworkManager.IsHost)
		{
			return;
		}

		Player player = ByPeer(peerId);

		if (player == null)
		{
			return;
		}

		RemoveSlot(player.Slot);

		Rebuild();
	}

	#endregion


	#region Roster

	public Player LocalPlayer
	{
		get
		{
			int local = NetworkManager.LocalPeerId;

			foreach (Player player in Roster)
			{
				if (!player.IsBot && player.PeerId == local)
				{
					return player;
				}
			}

			return null;
		}
	}


	public int HumanCount
	{
		get
		{
			int count = 0;

			foreach (Player player in Roster)
			{
				if (!player.IsBot)
				{
					count++;
				}
			}

			return count;
		}
	}


	private int BotCount
	{
		get
		{
			int count = 0;

			foreach (Player player in Roster)
			{
				if (player.IsBot)
				{
					count++;
				}
			}

			return count;
		}
	}


	private void AddPlayer(int slot, int peerId, ulong identity, string name, bool isHost)
	{
		if (slot < 0)
		{
			return;
		}

		RemoveSlot(slot);

		Roster.Add(new Player
		{
			Slot = slot,
			PeerId = peerId,
			SteamId = identity,
			Name = name,
			IsHost = isHost
		});
	}


	private void RemoveSlot(int slot)
	{
		for (int index = Roster.Count - 1; index >= 0; index--)
		{
			if (Roster[index].Slot == slot)
			{
				Roster.RemoveAt(index);
			}
		}

		FactionBySlot.Remove(slot);
		Editors.Remove(slot);
	}


	private Player BySlot(int slot)
	{
		foreach (Player player in Roster)
		{
			if (player.Slot == slot)
			{
				return player;
			}
		}

		return null;
	}


	private Player ByPeer(int peerId)
	{
		foreach (Player player in Roster)
		{
			if (!player.IsBot && player.PeerId == peerId)
			{
				return player;
			}
		}

		return null;
	}


	private int FreeSlot()
	{
		for (int slot = 0; slot < MaxPlayers; slot++)
		{
			if (BySlot(slot) == null)
			{
				return slot;
			}
		}

		return -1;
	}


	private void Rebuild()
	{
		if (!NetworkManager.IsHost)
		{
			return;
		}

		AssignFactions();
		ApplyAccess();
		Sort();

		if (NetworkManager.PeerCount > 0)
		{
			Rpc(MethodName.SyncRoster, RosterCodec.Pack(Roster, SessionName, MaxPlayers));
		}

		RosterChanged?.Invoke();
	}


	private void Sort()
	{
		int local = NetworkManager.LocalPeerId;

		List<Player> sorted = new();

		for (int rank = 0; rank <= 3; rank++)
		{
			foreach (Player player in Roster)
			{
				if (Rank(player, local) == rank)
				{
					sorted.Add(player);
				}
			}
		}

		Roster.Clear();
		Roster.AddRange(sorted);
	}


	private static int Rank(Player player, int local)
	{
		if (!player.IsBot && player.PeerId == local)
		{
			return 0;
		}

		if (player.IsHost)
		{
			return 1;
		}

		return player.IsBot ? 3 : 2;
	}

	#endregion


	#region Sync

	[Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void SyncRoster(byte[] data)
	{
		if (NetworkManager.IsHost)
		{
			return;
		}

		if (!RosterCodec.Unpack(data, Roster, out string name, out int maxPlayers))
		{
			GD.PushWarning("Session: malformed roster packet");
			return;
		}

		SessionName = name;
		MaxPlayers = maxPlayers;

		Sort();

		GD.Print($"[Session] roster received: {Roster.Count} players in \"{name}\"");

		RosterChanged?.Invoke();
	}

	#endregion


	#region Factions

	public bool CanUseSize(int maxFactionCount)
	{
		return HumanCount <= maxFactionCount;
	}


	public bool CanEditFaction(Player player)
	{
		return !player.IsBot && player.PeerId == NetworkManager.LocalPeerId;
	}


	public void ChangeFaction(Player player, int index)
	{
		if (index < 0 || index >= Factions.COUNT)
		{
			return;
		}

		if (NetworkManager.IsHost)
		{
			ApplyFaction(player.Slot, index);
			return;
		}

		if (player.PeerId == NetworkManager.LocalPeerId)
		{
			RpcId(NetworkManager.HOST_PEER, MethodName.RequestFaction, index);
		}
	}


	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RequestFaction(int index)
	{
		if (!NetworkManager.IsHost)
		{
			return;
		}

		Player player = ByPeer(NetworkManager.SenderId);

		if (player != null)
		{
			ApplyFaction(player.Slot, index);
		}
	}


	private void ApplyFaction(int slot, int index)
	{
		if (!NetworkManager.IsHost || index < 0 || index >= Factions.COUNT)
		{
			return;
		}

		int holder = FindHolder(index, slot);

		if (holder >= 0)
		{
			Player owner = BySlot(holder);

			if (owner == null || !owner.IsBot)
			{
				return;
			}
		}

		FactionBySlot[slot] = index;

		GD.Print($"[Session] slot {slot} took faction {Factions.Get(index).Title}");

		if (holder >= 0)
		{
			Displace(holder);
		}

		Rebuild();
	}


	private void AssignFactions()
	{
		HashSet<int> used = new(FactionBySlot.Values);

		foreach (Player player in Roster)
		{
			if (FactionBySlot.TryGetValue(player.Slot, out int existing))
			{
				player.FactionIndex = existing;
				continue;
			}

			int index = Factions.PickFree(used, FactionRand);

			FactionBySlot[player.Slot] = index;
			used.Add(index);

			player.FactionIndex = index;
		}
	}


	private void Displace(int slot)
	{
		int index = Factions.PickFree(new HashSet<int>(FactionBySlot.Values), FactionRand);

		FactionBySlot[slot] = index;
	}


	private int FindHolder(int index, int exclude)
	{
		foreach (KeyValuePair<int, int> pair in FactionBySlot)
		{
			if (pair.Key != exclude && pair.Value == index)
			{
				return pair.Key;
			}
		}

		return -1;
	}

	#endregion


	#region World access

	public bool CanEditWorld
	{
		get
		{
			if (NetworkManager.IsHost)
			{
				return true;
			}

			Player local = LocalPlayer;

			return local != null && local.CanEditWorld;
		}
	}


	public bool CanGrantWorld(Player player)
	{
		return NetworkManager.IsHost && !player.IsBot && !player.IsHost;
	}


	public void ToggleWorldAccess(Player player)
	{
		if (!CanGrantWorld(player))
		{
			return;
		}

		if (!Editors.Add(player.Slot))
		{
			Editors.Remove(player.Slot);
		}

		Rebuild();
	}


	private void ApplyAccess()
	{
		foreach (Player player in Roster)
		{
			player.CanEditWorld = player.IsHost || Editors.Contains(player.Slot);
		}
	}

	#endregion


	#region Kicking

	public bool CanKick(Player player)
	{
		return NetworkManager.IsHost && (player.IsBot || player.PeerId != NetworkManager.LocalPeerId);
	}


	public void Kick(Player player)
	{
		if (!CanKick(player))
		{
			return;
		}

		if (player.IsBot)
		{
			RemoveBot(player.Slot);
			return;
		}

		int peerId = player.PeerId;

		if (player.SteamId != 0UL)
		{
			Banned.Add(player.SteamId);
		}

		GD.Print($"[Session] kicking {player.Name} (peer {peerId})");

		RpcId(peerId, MethodName.Kicked, "You were kicked from the lobby.");

		RemoveSlot(player.Slot);

		Rebuild();

		GetTree().CreateTimer(KICK_DELAY).Timeout += () => NetworkManager.Instance?.Disconnect(peerId);
	}


	[Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void Kicked(string reason)
	{
		NetworkManager.Instance?.NoteFailure(reason);
	}

	#endregion


	#region Bots

	public bool CanAddBot()
	{
		return NetworkManager.IsHost && Roster.Count < MaxPlayers;
	}


	public bool CanRemoveBots()
	{
		return NetworkManager.IsHost && BotCount > 0;
	}


	public void AddBot()
	{
		if (!CanAddBot())
		{
			return;
		}

		SpawnBot();

		Rebuild();
	}


	public void FillWithBots()
	{
		if (!NetworkManager.IsHost)
		{
			return;
		}

		while (Roster.Count < MaxPlayers && SpawnBot())
		{
		}

		Rebuild();
	}


	public void RemoveBots()
	{
		if (!CanRemoveBots())
		{
			return;
		}

		for (int index = Roster.Count - 1; index >= 0; index--)
		{
			if (Roster[index].IsBot)
			{
				RemoveSlot(Roster[index].Slot);
			}
		}

		Rebuild();
	}


	public void RemoveBot(int slot)
	{
		Player player = BySlot(slot);

		if (NetworkManager.IsHost && player != null && player.IsBot)
		{
			RemoveSlot(slot);

			Rebuild();
		}
	}


	private bool SpawnBot()
	{
		int slot = FreeSlot();

		if (slot < 0)
		{
			return false;
		}

		Roster.Add(new Player
		{
			Slot = slot,
			Name = $"Bot {slot + 1}",
			IsBot = true
		});

		return true;
	}


	private bool RemoveOneBot()
	{
		for (int index = Roster.Count - 1; index >= 0; index--)
		{
			if (!Roster[index].IsBot)
			{
				continue;
			}

			RemoveSlot(Roster[index].Slot);

			return true;
		}

		return false;
	}


	private void TrimBots()
	{
		while (Roster.Count > MaxPlayers && RemoveOneBot())
		{
		}
	}

	#endregion
}
