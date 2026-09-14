using Godot;
using System;
using System.Collections.Generic;
using Steamworks;

public partial class SteamManager : Node
{
	#region Constants

	private const uint APP_ID = 480;
	private const string FALLBACK_NAME = "Player";

	public const string KEY_NAME = "name";
	public const string KEY_PING_LOCATION = "ping_location";
	public const string KEY_DEV_HOST = "dev_host";
	public const string KEY_HOST_ID = "host_id";

	private const string DEV_HOST = "RaRseR";

	private const int MAX_SEARCH_RESULTS = 200;
	private const int PING_LOCATION_BUFFER = 512;

	#endregion


	#region Events

	public static event Action<ulong> LobbyCreated;
	public static event Action<ulong> LobbyJoined;
	public static event Action LobbyLeft;
	public static event Action MembersChanged;
	public static event Action<List<SteamLobbyInfo>> LobbyListReceived;
	public static event Action<ulong> InviteAccepted;
	public static event Action<ulong> AvatarLoaded;

	#endregion


	#region State

	public static SteamManager Instance { get; private set; }

	public static bool IsReady { get; private set; }

	public static string PersonaName => IsReady ? SteamFriends.GetPersonaName() : FALLBACK_NAME;

	public static ulong LocalId => IsReady ? SteamUser.GetSteamID().m_SteamID : 0UL;

	public static ulong CurrentLobby { get; private set; }

	public static bool IsInLobby => CurrentLobby != 0UL;

	public static ulong HostId => IsReady && IsInLobby
		? SteamMatchmaking.GetLobbyOwner(new CSteamID(CurrentLobby)).m_SteamID
		: 0UL;

	public static bool IsLobbyOwner => IsInLobby && HostId == LocalId;

	private static readonly Dictionary<ulong, ImageTexture> AvatarCache = new();

	private CallResult<LobbyCreated_t> LobbyCreatedResult;
	private CallResult<LobbyMatchList_t> LobbyMatchListResult;
	private Callback<LobbyEnter_t> LobbyEnterCallback;
	private Callback<LobbyChatUpdate_t> LobbyChatUpdateCallback;
	private Callback<GameLobbyJoinRequested_t> JoinRequestedCallback;
	private Callback<AvatarImageLoaded_t> AvatarImageLoadedCallback;
	private Callback<PersonaStateChange_t> PersonaStateChangeCallback;

	private string PendingName;
	private int PendingMaxPlayers;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		Instance = this;

		try
		{
			if (SteamAPI.RestartAppIfNecessary(new AppId_t(APP_ID)))
			{
				GD.PrintErr("[Steam] relaunching through the Steam client, steam_appid.txt is missing");
				GetTree().Quit();
				return;
			}

			IsReady = SteamAPI.Init();

			if (IsReady)
			{
				RegisterCallbacks();
				GD.Print($"[Steam] ready: {PersonaName} ({LocalId})");
			}
			else
			{
				GD.PrintErr("Failed to initialize [Steam] SteamAPI.Init returned false");
			}
		}
		catch (Exception e)
		{
			GD.PrintErr("Failed to initialize [Steam] " + e.Message);
		}

		GD.Print($"[Steam] overlay enabled: {SteamUtils.IsOverlayEnabled()}");
	}


	public override void _Process(double delta)
	{
		if (IsReady)
		{
			SteamAPI.RunCallbacks();
		}
	}


	public override void _ExitTree()
	{
		Instance = null;

		if (!IsReady)
		{
			return;
		}

		LeaveLobby();

		AvatarCache.Clear();

		SteamAPI.Shutdown();
		IsReady = false;

		GD.Print("[Steam] shutdown");
	}


	private void RegisterCallbacks()
	{
		LobbyCreatedResult = CallResult<LobbyCreated_t>.Create(OnLobbyCreated);
		LobbyMatchListResult = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);

		LobbyEnterCallback = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
		LobbyChatUpdateCallback = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
		JoinRequestedCallback = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequested);
		AvatarImageLoadedCallback = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
		PersonaStateChangeCallback = Callback<PersonaStateChange_t>.Create(OnPersonaStateChange);
	}

	#endregion


	#region Lobby lifecycle

	public void CreateLobby(string name, int maxPlayers)
	{
		if (!IsReady)
		{
			return;
		}

		PendingName = name;
		PendingMaxPlayers = maxPlayers;

		GD.Print($"[Steam] creating lobby \"{name}\" for {maxPlayers}");

		LobbyCreatedResult.Set(SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, maxPlayers));
	}


	public void JoinLobby(ulong lobbyId)
	{
		if (IsReady)
		{
			GD.Print($"[Steam] joining lobby {lobbyId}");

			SteamMatchmaking.JoinLobby(new CSteamID(lobbyId));
		}
	}


	public void LeaveLobby()
	{
		if (!IsReady || !IsInLobby)
		{
			return;
		}

		GD.Print($"[Steam] leaving lobby {CurrentLobby}");

		SteamMatchmaking.LeaveLobby(new CSteamID(CurrentLobby));

		CurrentLobby = 0UL;

		LobbyLeft?.Invoke();
	}


	public void CloseLobby()
	{
		if (IsLobbyOwner)
		{
			GD.Print($"[Steam] lobby {CurrentLobby} closed for new players");

			SteamMatchmaking.SetLobbyJoinable(new CSteamID(CurrentLobby), false);
		}
	}


	public void InviteFriend()
	{
		if (IsInLobby)
		{
			GD.Print("[Steam] invite overlay opened");

			SteamFriends.ActivateGameOverlayInviteDialog(new CSteamID(CurrentLobby));
		}
	}


	#endregion


	#region Lobby data

	public void PublishLobby(string name, int maxPlayers)
	{
		if (!IsLobbyOwner)
		{
			return;
		}

		CSteamID lobby = new CSteamID(CurrentLobby);

		SteamMatchmaking.SetLobbyData(lobby, KEY_NAME, name);
		SteamMatchmaking.SetLobbyData(lobby, KEY_PING_LOCATION, GetLocalPingLocation());
		SteamMatchmaking.SetLobbyData(lobby, KEY_DEV_HOST, DEV_HOST);
		SteamMatchmaking.SetLobbyData(lobby, KEY_HOST_ID, LocalId.ToString());

		SteamMatchmaking.SetLobbyJoinable(lobby, true);
		SteamMatchmaking.SetLobbyMemberLimit(lobby, maxPlayers);

		GD.Print($"[Steam] lobby data published: \"{name}\", limit {maxPlayers}");
	}


	public List<ulong> GetMemberIds()
	{
		List<ulong> members = new();

		if (!IsReady || !IsInLobby)
		{
			return members;
		}

		CSteamID lobby = new CSteamID(CurrentLobby);

		int count = SteamMatchmaking.GetNumLobbyMembers(lobby);

		for (int index = 0; index < count; index++)
		{
			members.Add(SteamMatchmaking.GetLobbyMemberByIndex(lobby, index).m_SteamID);
		}

		return members;
	}


	public static string GetMemberName(ulong steamId)
	{
		return IsReady ? SteamFriends.GetFriendPersonaName(new CSteamID(steamId)) : FALLBACK_NAME;
	}

	#endregion


	#region Avatars

	public static Texture2D GetAvatar(ulong steamId)
	{
		if (!IsReady || steamId == 0UL)
		{
			return null;
		}

		if (AvatarCache.TryGetValue(steamId, out ImageTexture cached))
		{
			return cached;
		}

		CSteamID id = new CSteamID(steamId);
		int handle = SteamFriends.GetMediumFriendAvatar(id);

		if (handle == -1)
		{
			SteamFriends.RequestUserInformation(id, false);
			return null;
		}

		if (handle == 0)
		{
			return null;
		}

		return BuildAvatar(steamId, handle);
	}


	private static ImageTexture BuildAvatar(ulong steamId, int handle)
	{
		if (!SteamUtils.GetImageSize(handle, out uint width, out uint height))
		{
			return null;
		}

		if (width == 0 || height == 0)
		{
			return null;
		}

		byte[] buffer = new byte[width * height * 4];

		if (!SteamUtils.GetImageRGBA(handle, buffer, buffer.Length))
		{
			return null;
		}

		Image image = Image.CreateFromData((int)width, (int)height, false, Image.Format.Rgba8, buffer);
		ImageTexture texture = ImageTexture.CreateFromImage(image);

		AvatarCache[steamId] = texture;

		return texture;
	}


	private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
	{
		ulong id = callback.m_steamID.m_SteamID;

		AvatarCache.Remove(id);
		AvatarLoaded?.Invoke(id);
	}


	private void OnPersonaStateChange(PersonaStateChange_t callback)
	{
		EPersonaChange interesting = EPersonaChange.k_EPersonaChangeAvatar | EPersonaChange.k_EPersonaChangeName;

		if ((callback.m_nChangeFlags & interesting) == 0)
		{
			return;
		}

		AvatarCache.Remove(callback.m_ulSteamID);
		AvatarLoaded?.Invoke(callback.m_ulSteamID);
	}

	#endregion


	#region Ping

	public static string GetLocalPingLocation()
	{
		if (!IsReady)
		{
			return string.Empty;
		}

		if (SteamNetworkingUtils.GetLocalPingLocation(out SteamNetworkPingLocation_t location) < 0f)
		{
			return string.Empty;
		}

		SteamNetworkingUtils.ConvertPingLocationToString(ref location, out string text, PING_LOCATION_BUFFER);

		return text ?? string.Empty;
	}


	public static int EstimatePing(string pingLocation)
	{
		if (!IsReady || string.IsNullOrEmpty(pingLocation))
		{
			return -1;
		}

		if (!SteamNetworkingUtils.ParsePingLocationString(pingLocation, out SteamNetworkPingLocation_t location))
		{
			return -1;
		}

		return SteamNetworkingUtils.EstimatePingTimeFromLocalHost(ref location);
	}

	#endregion


	#region Search

	public void FindLobbies(SteamLobbyQuery query)
	{
		if (!IsReady)
		{
			LobbyListReceived?.Invoke(new List<SteamLobbyInfo>());
			return;
		}

		GD.Print("[Steam] searching lobbies");

		SteamMatchmaking.AddRequestLobbyListStringFilter(
			KEY_DEV_HOST, DEV_HOST, ELobbyComparison.k_ELobbyComparisonEqual
		);

		SteamMatchmaking.AddRequestLobbyListResultCountFilter(MAX_SEARCH_RESULTS);
		SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);

		if (query.MinFreeSlots > 0)
		{
			SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(query.MinFreeSlots);
		}

		LobbyMatchListResult.Set(SteamMatchmaking.RequestLobbyList());
	}


	private void OnLobbyMatchList(LobbyMatchList_t callback, bool ioFailure)
	{
		List<SteamLobbyInfo> lobbies = new();

		if (ioFailure)
		{
			LobbyListReceived?.Invoke(lobbies);
			return;
		}

		for (int index = 0; index < callback.m_nLobbiesMatching; index++)
		{
			lobbies.Add(ReadLobby(SteamMatchmaking.GetLobbyByIndex(index)));
		}

		GD.Print($"[Steam] found {lobbies.Count} lobbies");

		LobbyListReceived?.Invoke(lobbies);
	}


	private static SteamLobbyInfo ReadLobby(CSteamID id)
	{
		string name = SteamMatchmaking.GetLobbyData(id, KEY_NAME);
		string pingLocation = SteamMatchmaking.GetLobbyData(id, KEY_PING_LOCATION);
		string host = SteamMatchmaking.GetLobbyData(id, KEY_HOST_ID);

		return new SteamLobbyInfo
		{
			Id = id.m_SteamID,
			HostId = ulong.TryParse(host, out ulong hostId) ? hostId : SteamMatchmaking.GetLobbyOwner(id).m_SteamID,
			Name = name.Length > 0 ? name : "Unnamed lobby",
			MaxPlayers = SteamMatchmaking.GetLobbyMemberLimit(id),
			PlayerCount = SteamMatchmaking.GetNumLobbyMembers(id),
			Ping = EstimatePing(pingLocation)
		};
	}

	#endregion


	#region Steam Callbacks

	private void OnLobbyCreated(LobbyCreated_t callback, bool ioFailure)
	{
		if (ioFailure || callback.m_eResult != EResult.k_EResultOK)
		{
			GD.PushWarning($"[Steam] failed to create lobby - {callback.m_eResult}");
			return;
		}

		CurrentLobby = callback.m_ulSteamIDLobby;

		if (PendingName != null)
		{
			PublishLobby(PendingName, PendingMaxPlayers);
			PendingName = null;
		}

		GD.Print($"[Steam] lobby created: {CurrentLobby}");

		LobbyCreated?.Invoke(CurrentLobby);
	}


	private void OnLobbyEnter(LobbyEnter_t callback)
	{
		if (callback.m_EChatRoomEnterResponse != (uint)EChatRoomEnterResponse.k_EChatRoomEnterResponseSuccess)
		{
			GD.PushWarning($"[Steam] failed to enter lobby, response {callback.m_EChatRoomEnterResponse}");
			return;
		}

		CurrentLobby = callback.m_ulSteamIDLobby;

		GD.Print($"[Steam] entered lobby {CurrentLobby}, host {GetMemberName(HostId)}, members {GetMemberIds().Count}");

		LobbyJoined?.Invoke(CurrentLobby);
		MembersChanged?.Invoke();
	}


	private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
	{
		GD.Print($"[Steam] lobby members changed: {GetMemberIds().Count}");

		MembersChanged?.Invoke();
	}


	private void OnJoinRequested(GameLobbyJoinRequested_t callback)
	{
		GD.Print($"[Steam] invite accepted for lobby {callback.m_steamIDLobby.m_SteamID}");

		InviteAccepted?.Invoke(callback.m_steamIDLobby.m_SteamID);
	}

	#endregion
}
