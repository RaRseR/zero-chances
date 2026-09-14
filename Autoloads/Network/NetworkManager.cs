using Godot;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

public partial class NetworkManager : Node
{
	#region Constants

	public const int HOST_PEER = 1;

	private const double AUTH_TIMEOUT = 5.0;
	private const int NONCE_SIZE = 16;
	private const float DROP_DELAY = 0.25f;

	private const byte TAG_NONCE = 1;
	private const byte TAG_CREDENTIALS = 2;
	private const byte TAG_REJECT = 3;

	private const string FALLBACK_NAME = "Player";

	#endregion


	#region Events

	public static event Action<int> PeerJoined;
	public static event Action<int> PeerLeft;
	public static event Action<string> Failed;
	public static event Action Closed;

	#endregion


	#region State

	public static NetworkManager Instance { get; private set; }

	public static bool IsActive { get; private set; }
	public static bool IsHost { get; private set; }

	public static int LocalPeerId => IsActive && Instance != null ? Instance.Multiplayer.GetUniqueId() : 0;

	public static int SenderId => Instance != null ? Instance.Multiplayer.GetRemoteSenderId() : 0;

	public static int PeerCount => IsActive && Instance != null ? Instance.Multiplayer.GetPeers().Length : 0;

	public static ulong LocalIdentity => SteamManager.LocalId;

	public static string LocalName => SteamManager.PersonaName;

	public Func<ulong, string, string> Admit;

	private string Password = string.Empty;
	private string PendingPassword = string.Empty;
	private string RejectReason = string.Empty;

	private readonly Dictionary<int, ulong> IdentityByPeer = new();
	private readonly Dictionary<int, string> NameByPeer = new();
	private readonly Dictionary<int, byte[]> NonceByPeer = new();

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		Instance = this;

		Multiplayer.PeerConnected += OnPeerConnected;
		Multiplayer.PeerDisconnected += OnPeerDisconnected;
		Multiplayer.ConnectedToServer += OnConnectedToServer;
		Multiplayer.ConnectionFailed += OnConnectionFailed;
		Multiplayer.ServerDisconnected += OnServerDisconnected;

		if (Multiplayer is SceneMultiplayer scene)
		{
			scene.AuthCallback = Callable.From<int, byte[]>(OnAuth);
			scene.AuthTimeout = AUTH_TIMEOUT;

			scene.PeerAuthenticating += OnPeerAuthenticating;
			scene.PeerAuthenticationFailed += OnPeerAuthenticationFailed;
		}
	}


	public override void _ExitTree()
	{
		Multiplayer.PeerConnected -= OnPeerConnected;
		Multiplayer.PeerDisconnected -= OnPeerDisconnected;
		Multiplayer.ConnectedToServer -= OnConnectedToServer;
		Multiplayer.ConnectionFailed -= OnConnectionFailed;
		Multiplayer.ServerDisconnected -= OnServerDisconnected;

		if (Multiplayer is SceneMultiplayer scene)
		{
			scene.PeerAuthenticating -= OnPeerAuthenticating;
			scene.PeerAuthenticationFailed -= OnPeerAuthenticationFailed;
		}

		Instance = null;
	}

	#endregion


	#region Lifecycle

	public bool Host(string password)
	{
		Close();

		if (!SteamManager.IsReady)
		{
			Fail("Steam is not running.");
			return false;
		}

		NetworkMultiplayerPeer peer = new NetworkMultiplayerPeer();

		peer.CreateHost();

		Password = password ?? string.Empty;
		IsHost = true;
		IsActive = true;

		Multiplayer.MultiplayerPeer = peer;

		GD.Print($"[Network] hosting, peer id {Multiplayer.GetUniqueId()}");

		return true;
	}


	public bool Join(ulong hostSteamId, string password)
	{
		Close();

		if (!SteamManager.IsReady)
		{
			Fail("Steam is not running.");
			return false;
		}

		if (hostSteamId == 0UL)
		{
			Fail("This lobby has no host.");
			return false;
		}

		NetworkMultiplayerPeer peer = new NetworkMultiplayerPeer();

		peer.CreateClient(hostSteamId);

		PendingPassword = password ?? string.Empty;
		IsHost = false;
		IsActive = true;
		RejectReason = string.Empty;

		Multiplayer.MultiplayerPeer = peer;

		GD.Print($"[Network] connecting to host {hostSteamId}");

		return true;
	}


	public void Close()
	{
		bool wasActive = IsActive;

		if (wasActive)
		{
			GD.Print("[Network] closing");
		}

		Multiplayer.MultiplayerPeer?.Close();
		Multiplayer.MultiplayerPeer = null;

		IdentityByPeer.Clear();
		NameByPeer.Clear();
		NonceByPeer.Clear();

		Password = string.Empty;
		PendingPassword = string.Empty;

		IsActive = false;
		IsHost = false;

		if (wasActive)
		{
			Closed?.Invoke();
		}
	}


	public void Disconnect(int peerId)
	{
		if (IsHost && IsActive && peerId != HOST_PEER)
		{
			GD.Print($"[Network] dropping peer {peerId}");

			Multiplayer.MultiplayerPeer?.DisconnectPeer(peerId);
		}
	}


	public void SetPassword(string password)
	{
		if (IsHost)
		{
			Password = password ?? string.Empty;
		}
	}


	public void NoteFailure(string reason)
	{
		RejectReason = reason ?? string.Empty;
	}


	private void Fail(string reason)
	{
		GD.Print($"[Network] failed: {reason}");

		Close();

		Failed?.Invoke(reason);
	}

	#endregion


	#region Identity

	public ulong IdentityOf(int peerId)
	{
		return IdentityByPeer.TryGetValue(peerId, out ulong identity) ? identity : 0UL;
	}


	public string NameOf(int peerId)
	{
		return NameByPeer.TryGetValue(peerId, out string name) ? name : FALLBACK_NAME;
	}

	#endregion


	#region Authentication

	private void OnPeerAuthenticating(long id)
	{
		if (!IsHost)
		{
			return;
		}

		byte[] nonce = System.Security.Cryptography.RandomNumberGenerator.GetBytes(NONCE_SIZE);

		NonceByPeer[(int)id] = nonce;

		MessageWriter writer = new MessageWriter();

		writer.Byte(TAG_NONCE);
		writer.Blob(nonce);

		SendAuth((int)id, writer.ToArray());
	}


	private void OnAuth(int peerId, byte[] data)
	{
		MessageReader reader = new MessageReader(data);

		byte tag = reader.Byte();

		if (IsHost)
		{
			ReadCredentials(peerId, tag, reader);
			return;
		}

		if (tag == TAG_REJECT)
		{
			RejectReason = reader.Text();
			return;
		}

		if (tag != TAG_NONCE)
		{
			return;
		}

		byte[] nonce = reader.Blob();

		if (reader.Failed)
		{
			return;
		}

		MessageWriter writer = new MessageWriter();

		writer.Byte(TAG_CREDENTIALS);
		writer.ULong(LocalIdentity);
		writer.Text(LocalName);
		writer.Blob(Proof(nonce, PendingPassword));

		SendAuth(HOST_PEER, writer.ToArray());

		CompleteAuth(HOST_PEER);
	}


	private void ReadCredentials(int peerId, byte tag, MessageReader reader)
	{
		if (tag != TAG_CREDENTIALS || !NonceByPeer.TryGetValue(peerId, out byte[] nonce))
		{
			Reject(peerId, "Handshake failed.");
			return;
		}

		ulong identity = reader.ULong();
		string name = reader.Text();
		byte[] proof = reader.Blob();

		if (reader.Failed || identity == 0UL)
		{
			Reject(peerId, "Handshake failed.");
			return;
		}

		if (!CryptographicOperations.FixedTimeEquals(Proof(nonce, Password), proof))
		{
			Reject(peerId, "Wrong password.");
			return;
		}

		string denial = Admit?.Invoke(identity, name);

		if (denial != null)
		{
			Reject(peerId, denial);
			return;
		}

		IdentityByPeer[peerId] = identity;
		NameByPeer[peerId] = name.Length > 0 ? name : FALLBACK_NAME;

		NonceByPeer.Remove(peerId);

		GD.Print($"[Network] peer {peerId} authenticated as {NameByPeer[peerId]}");

		CompleteAuth(peerId);
	}


	private void Reject(int peerId, string reason)
	{
		GD.Print($"[Network] peer {peerId} rejected: {reason}");

		MessageWriter writer = new MessageWriter();

		writer.Byte(TAG_REJECT);
		writer.Text(reason);

		SendAuth(peerId, writer.ToArray());

		NonceByPeer.Remove(peerId);

		GetTree().CreateTimer(DROP_DELAY).Timeout += () => Disconnect(peerId);
	}


	private void SendAuth(int peerId, byte[] data)
	{
		if (Multiplayer is SceneMultiplayer scene)
		{
			scene.SendAuth(peerId, data);
		}
	}


	private void CompleteAuth(int peerId)
	{
		if (Multiplayer is SceneMultiplayer scene)
		{
			scene.CompleteAuth(peerId);
		}
	}


	private static byte[] Proof(byte[] nonce, string password)
	{
		byte[] secret = Encoding.UTF8.GetBytes(password ?? string.Empty);
		byte[] input = new byte[nonce.Length + secret.Length];

		Buffer.BlockCopy(nonce, 0, input, 0, nonce.Length);
		Buffer.BlockCopy(secret, 0, input, nonce.Length, secret.Length);

		return SHA256.HashData(input);
	}

	#endregion


	#region Multiplayer Callbacks

	private void OnPeerConnected(long id)
	{
		GD.Print($"[Network] peer {id} connected");

		PeerJoined?.Invoke((int)id);
	}


	private void OnPeerDisconnected(long id)
	{
		int peerId = (int)id;

		GD.Print($"[Network] peer {peerId} disconnected");

		IdentityByPeer.Remove(peerId);
		NameByPeer.Remove(peerId);
		NonceByPeer.Remove(peerId);

		PeerLeft?.Invoke(peerId);
	}


	private void OnConnectedToServer()
	{
		GD.Print($"[Network] joined as peer {Multiplayer.GetUniqueId()}");
	}


	private void OnConnectionFailed()
	{
		Fail(RejectReason.Length > 0 ? RejectReason : "Could not reach the host.");
	}


	private void OnServerDisconnected()
	{
		Fail(RejectReason.Length > 0 ? RejectReason : "Disconnected from the host.");
	}


	private void OnPeerAuthenticationFailed(long id)
	{
		if (IsHost)
		{
			NonceByPeer.Remove((int)id);
			return;
		}

		Fail(RejectReason.Length > 0 ? RejectReason : "The host did not respond.");
	}

	#endregion
}
