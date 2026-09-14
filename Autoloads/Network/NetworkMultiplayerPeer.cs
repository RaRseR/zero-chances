using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Steamworks;

public partial class NetworkMultiplayerPeer : MultiplayerPeerExtension
{
	#region Constants

	public const int HOST_PEER = 1;
	public const int CHANNEL_COUNT = 5;

	private const int CONTROL_CHANNEL = 0;
	private const int FIRST_PEER = 2;
	private const int MAX_MESSAGES = 64;
	private const int MAX_PACKET_SIZE = 512 * 1024;
	private const int HEADER_SIZE = 8;

	private const byte TAG_HELLO = 1;
	private const byte TAG_WELCOME = 2;
	private const byte TAG_BYE = 3;

	private const int TARGET_ALL = 0;

	#endregion


	#region State

	private bool IsServer;
	private int LocalId;
	private ConnectionStatus Status = ConnectionStatus.Disconnected;

	private CSteamID HostId;
	private int NextPeerId = FIRST_PEER;

	private readonly Dictionary<int, CSteamID> SteamIdByPeer = new();
	private readonly Dictionary<ulong, int> PeerBySteamId = new();

	private readonly Queue<NetworkPacket> Incoming = new();
	private readonly IntPtr[] Messages = new IntPtr[MAX_MESSAGES];

	private int TargetPeer = TARGET_ALL;
	private int TransferChannel;
	private TransferModeEnum Transfer = TransferModeEnum.Reliable;

	private bool Refusing;

	private Callback<SteamNetworkingMessagesSessionRequest_t> SessionRequestCallback;
	private Callback<SteamNetworkingMessagesSessionFailed_t> SessionFailedCallback;

	#endregion


	#region Construction

	public void CreateHost()
	{
		IsServer = true;
		LocalId = HOST_PEER;
		HostId = SteamUser.GetSteamID();
		Status = ConnectionStatus.Connected;

		RegisterCallbacks();

		GD.Print("[SteamPeer] hosting");
	}


	public void CreateClient(ulong hostSteamId)
	{
		IsServer = false;
		LocalId = 0;
		HostId = new CSteamID(hostSteamId);
		Status = ConnectionStatus.Connecting;

		RegisterCallbacks();

		Send(HostId, CONTROL_CHANNEL, Control(TAG_HELLO, 0), true);

		GD.Print($"[SteamPeer] connecting to {hostSteamId}");
	}


	private void RegisterCallbacks()
	{
		SessionRequestCallback ??= Callback<SteamNetworkingMessagesSessionRequest_t>.Create(OnSessionRequest);
		SessionFailedCallback ??= Callback<SteamNetworkingMessagesSessionFailed_t>.Create(OnSessionFailed);
	}

	#endregion


	#region Steam callbacks

	private void OnSessionRequest(SteamNetworkingMessagesSessionRequest_t callback)
	{
		SteamNetworkingIdentity remote = callback.m_identityRemote;
		CSteamID sender = remote.GetSteamID();

		if (!IsServer && sender != HostId)
		{
			GD.Print($"[SteamPeer] refusing session from {sender}");
			return;
		}

		if (IsServer && Refusing)
		{
			return;
		}

		SteamNetworkingMessages.AcceptSessionWithUser(ref remote);

		GD.Print($"[SteamPeer] session accepted with {sender}");
	}


	private void OnSessionFailed(SteamNetworkingMessagesSessionFailed_t callback)
	{
		CSteamID remote = callback.m_info.m_identityRemote.GetSteamID();

		GD.Print($"[SteamPeer] session failed with {remote}");

		if (!IsServer)
		{
			Status = ConnectionStatus.Disconnected;
			return;
		}

		if (PeerBySteamId.TryGetValue(remote.m_SteamID, out int peerId))
		{
			Drop(peerId);
		}
	}

	#endregion


	#region Sending

	private static byte[] Control(byte tag, int value)
	{
		byte[] data = new byte[5];

		data[0] = tag;
		data[1] = (byte)value;
		data[2] = (byte)(value >> 8);
		data[3] = (byte)(value >> 16);
		data[4] = (byte)(value >> 24);

		return data;
	}


	private bool Send(CSteamID target, int channel, byte[] data, bool reliable)
	{
		if (data == null || data.Length == 0)
		{
			return false;
		}

		SteamNetworkingIdentity identity = default;
		identity.SetSteamID(target);

		IntPtr buffer = Marshal.AllocHGlobal(data.Length);

		try
		{
			Marshal.Copy(data, 0, buffer, data.Length);

			int flags = reliable
				? Constants.k_nSteamNetworkingSend_Reliable
				: Constants.k_nSteamNetworkingSend_UnreliableNoDelay;

			EResult result = SteamNetworkingMessages.SendMessageToUser(ref identity, buffer, (uint)data.Length, flags, channel);

			return result == EResult.k_EResultOK;
		}
		finally
		{
			Marshal.FreeHGlobal(buffer);
		}
	}


	private byte[] Wrap(int from, int to, byte[] payload)
	{
		byte[] framed = new byte[HEADER_SIZE + payload.Length];

		WriteInt(framed, 0, from);
		WriteInt(framed, 4, to);

		Buffer.BlockCopy(payload, 0, framed, HEADER_SIZE, payload.Length);

		return framed;
	}


	private static void WriteInt(byte[] target, int offset, int value)
	{
		target[offset] = (byte)value;
		target[offset + 1] = (byte)(value >> 8);
		target[offset + 2] = (byte)(value >> 16);
		target[offset + 3] = (byte)(value >> 24);
	}


	private static int ReadInt(byte[] source, int offset)
	{
		return source[offset]
			| (source[offset + 1] << 8)
			| (source[offset + 2] << 16)
			| (source[offset + 3] << 24);
	}


	private void Deliver(int from, int to, byte[] payload, int channel, TransferModeEnum mode)
	{
		bool reliable = mode == TransferModeEnum.Reliable;

		if (!IsServer)
		{
			Send(HostId, channel, Wrap(from, to, payload), reliable);
			return;
		}

		foreach (KeyValuePair<int, CSteamID> pair in SteamIdByPeer)
		{
			if (pair.Key == from)
			{
				continue;
			}

			if (to == TARGET_ALL || to == pair.Key || (to < 0 && -to != pair.Key))
			{
				Send(pair.Value, channel, Wrap(from, pair.Key, payload), reliable);
			}
		}
	}

	#endregion


	#region Receiving

	private void Receive(int channel)
	{
		int count = SteamNetworkingMessages.ReceiveMessagesOnChannel(channel, Messages, MAX_MESSAGES);

		for (int index = 0; index < count; index++)
		{
			IntPtr pointer = Messages[index];

			try
			{
				SteamNetworkingMessage_t message = SteamNetworkingMessage_t.FromIntPtr(pointer);

				byte[] data = new byte[message.m_cbSize];

				Marshal.Copy(message.m_pData, data, 0, message.m_cbSize);

				CSteamID sender = message.m_identityPeer.GetSteamID();

				if (channel == CONTROL_CHANNEL)
				{
					HandleControl(sender, data);
				}
				else
				{
					HandleGame(sender, channel, data);
				}
			}
			finally
			{
				SteamNetworkingMessage_t.Release(pointer);
			}
		}
	}


	private void HandleControl(CSteamID sender, byte[] data)
	{
		if (data.Length < 5)
		{
			return;
		}

		byte tag = data[0];
		int value = ReadInt(data, 1);

		if (tag == TAG_HELLO && IsServer)
		{
			Welcome(sender);
			return;
		}

		if (tag == TAG_WELCOME && !IsServer && Status == ConnectionStatus.Connecting)
		{
			LocalId = value;
			Status = ConnectionStatus.Connected;

			GD.Print($"[SteamPeer] welcomed as peer {LocalId}");

			EmitSignal(MultiplayerPeer.SignalName.PeerConnected, HOST_PEER);
			return;
		}

		if (tag == TAG_BYE)
		{
			if (IsServer && PeerBySteamId.TryGetValue(sender.m_SteamID, out int peerId))
			{
				Drop(peerId);
				return;
			}

			if (!IsServer)
			{
				Status = ConnectionStatus.Disconnected;

				EmitSignal(MultiplayerPeer.SignalName.PeerDisconnected, HOST_PEER);
			}
		}
	}


	private void Welcome(CSteamID sender)
	{
		if (Refusing)
		{
			return;
		}

		if (PeerBySteamId.TryGetValue(sender.m_SteamID, out int existing))
		{
			Send(sender, CONTROL_CHANNEL, Control(TAG_WELCOME, existing), true);
			return;
		}

		int peerId = NextPeerId++;

		SteamIdByPeer[peerId] = sender;
		PeerBySteamId[sender.m_SteamID] = peerId;

		Send(sender, CONTROL_CHANNEL, Control(TAG_WELCOME, peerId), true);

		GD.Print($"[SteamPeer] {sender} joined as peer {peerId}");

		EmitSignal(MultiplayerPeer.SignalName.PeerConnected, peerId);
	}


	private void HandleGame(CSteamID sender, int channel, byte[] data)
	{
		if (data.Length < HEADER_SIZE)
		{
			return;
		}

		int from = ReadInt(data, 0);
		int to = ReadInt(data, 4);

		byte[] payload = new byte[data.Length - HEADER_SIZE];

		Buffer.BlockCopy(data, HEADER_SIZE, payload, 0, payload.Length);

		if (IsServer && from != HOST_PEER)
		{
			if (!PeerBySteamId.TryGetValue(sender.m_SteamID, out int claimed) || claimed != from)
			{
				return;
			}
		}

		bool mine = to == TARGET_ALL || to == LocalId || (to < 0 && -to != LocalId);

		if (mine)
		{
			Incoming.Enqueue(new NetworkPacket
			{
				Data = payload,
				From = from,
				Channel = channel,
				Mode = TransferModeEnum.Reliable
			});
		}

		if (IsServer && to != HOST_PEER)
		{
			Deliver(from, to, payload, channel, TransferModeEnum.Reliable);
		}
	}


	private void Drop(int peerId)
	{
		if (!SteamIdByPeer.TryGetValue(peerId, out CSteamID steamId))
		{
			return;
		}

		SteamNetworkingIdentity identity = default;
		identity.SetSteamID(steamId);

		SteamNetworkingMessages.CloseSessionWithUser(ref identity);

		SteamIdByPeer.Remove(peerId);
		PeerBySteamId.Remove(steamId.m_SteamID);

		GD.Print($"[SteamPeer] peer {peerId} dropped");

		EmitSignal(MultiplayerPeer.SignalName.PeerDisconnected, peerId);
	}

	#endregion


	#region MultiplayerPeer

	public override void _Poll()
	{
		if (Status == ConnectionStatus.Disconnected)
		{
			return;
		}

		for (int channel = 0; channel < CHANNEL_COUNT; channel++)
		{
			Receive(channel);
		}
	}


	public override Error _PutPacketScript(byte[] pBuffer)
	{
		if (Status != ConnectionStatus.Connected || pBuffer == null || pBuffer.Length == 0)
		{
			return Error.Unconfigured;
		}

		Deliver(LocalId, TargetPeer, pBuffer, TransferChannel, Transfer);

		return Error.Ok;
	}


	public override byte[] _GetPacketScript()
	{
		return Incoming.Count > 0 ? Incoming.Dequeue().Data : Array.Empty<byte>();
	}


	public override int _GetAvailablePacketCount()
	{
		return Incoming.Count;
	}


	public override int _GetPacketPeer()
	{
		return Incoming.Count > 0 ? Incoming.Peek().From : 0;
	}


	public override int _GetPacketChannel()
	{
		return Incoming.Count > 0 ? Incoming.Peek().Channel : 0;
	}


	public override TransferModeEnum _GetPacketMode()
	{
		return Incoming.Count > 0 ? Incoming.Peek().Mode : TransferModeEnum.Reliable;
	}


	public override int _GetMaxPacketSize()
	{
		return MAX_PACKET_SIZE;
	}


	public override void _SetTargetPeer(int pPeer)
	{
		TargetPeer = pPeer;
	}


	public override void _SetTransferChannel(int pChannel)
	{
		TransferChannel = Mathf.Clamp(pChannel, 0, CHANNEL_COUNT - 1);
	}


	public override int _GetTransferChannel()
	{
		return TransferChannel;
	}


	public override void _SetTransferMode(TransferModeEnum pMode)
	{
		Transfer = pMode;
	}


	public override TransferModeEnum _GetTransferMode()
	{
		return Transfer;
	}


	public override int _GetUniqueId()
	{
		return LocalId;
	}


	public override bool _IsServer()
	{
		return IsServer;
	}


	public override bool _IsServerRelaySupported()
	{
		return true;
	}


	public override ConnectionStatus _GetConnectionStatus()
	{
		return Status;
	}


	public override void _SetRefuseNewConnections(bool pEnable)
	{
		Refusing = pEnable;
	}


	public override bool _IsRefusingNewConnections()
	{
		return Refusing;
	}


	public override void _DisconnectPeer(int pPeer, bool pForce)
	{
		if (SteamIdByPeer.TryGetValue(pPeer, out CSteamID steamId))
		{
			Send(steamId, CONTROL_CHANNEL, Control(TAG_BYE, 0), true);
		}

		Drop(pPeer);
	}


	public override void _Close()
	{
		if (!IsServer && Status == ConnectionStatus.Connected)
		{
			Send(HostId, CONTROL_CHANNEL, Control(TAG_BYE, 0), true);
		}

		foreach (CSteamID steamId in SteamIdByPeer.Values)
		{
			SteamNetworkingIdentity identity = default;
			identity.SetSteamID(steamId);

			SteamNetworkingMessages.CloseSessionWithUser(ref identity);
		}

		SteamIdByPeer.Clear();
		PeerBySteamId.Clear();
		Incoming.Clear();

		Status = ConnectionStatus.Disconnected;
		LocalId = 0;
		NextPeerId = FIRST_PEER;

		GD.Print("[SteamPeer] closed");
	}

	#endregion
}
