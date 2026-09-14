using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public enum MatchPhase
{
	Idle,
	Generating,
	Receiving,
	Playing,
	Finished
}


public partial class MatchManager : Node
{
	#region Constants

	private const string MATCH_SCENE = "res://Match/View/MatchScene.tscn";
	private const string MENU_SCENE = "res://UI/Menu/MenuRoot.tscn";

	private const int CHUNK_SIZE = 48 * 1024;
	private const int CHUNKS_PER_FRAME = 2;

	#endregion


	#region Events

	public static event Action PhaseChanged;
	public static event Action<float> ProgressChanged;

	#endregion


	#region State

	public static MatchManager Instance { get; private set; }

	public MatchPhase Phase { get; private set; } = MatchPhase.Idle;

	public MatchState State { get; private set; }

	private readonly Queue<byte[]> Outgoing = new();
	private int OutgoingTotal;
	private int OutgoingIndex;

	private readonly List<byte[]> Incoming = new();
	private int IncomingTotal;

	private SessionConfiguration PendingConfiguration;

	private WorldGeneration Generated;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		Instance = this;

		NetworkManager.Closed += OnNetworkClosed;
	}


	public override void _ExitTree()
	{
		NetworkManager.Closed -= OnNetworkClosed;

		Instance = null;
	}


	public override void _Process(double delta)
	{
		PumpChunks();
	}

	#endregion


	#region Host

	public bool StartMatch(SessionConfiguration configuration)
	{
		if (configuration == null || !NetworkManager.IsHost || Phase != MatchPhase.Idle)
		{
			return false;
		}

		Reset();

		PendingConfiguration = configuration;

		SetPhase(MatchPhase.Generating);

		GD.Print($"[Match] generating, seed {configuration.Seed}, side {configuration.PointCountPerWorldSide}");

		if (NetworkManager.PeerCount > 0)
		{
			Rpc(MethodName.BeginMatch, MatchCodec.EncodeConfiguration(configuration));
		}

		Task.Run(() =>
		{
			WorldGeneration generation = Generator.Generate(configuration);

			Generated = generation;

			Callable.From(OnHostGenerated).CallDeferred();
		});

		return true;
	}


	private void OnHostGenerated()
	{
		WorldGeneration generation = Generated;

		Generated = null;

		if (generation == null || Phase != MatchPhase.Generating)
		{
			return;
		}

		State = new MatchState(PendingConfiguration, generation.Layers);

		GD.Print($"[Match] world ready in {generation.TotalMilliseconds:F0} ms");

		if (NetworkManager.PeerCount > 0)
		{
			SessionConfiguration configuration = State.Configuration;
			WorldLayers layers = generation.Layers;

			Task.Run(() =>
			{
				byte[] snapshot = MatchCodec.EncodeWorld(layers, configuration);

				Callable.From(() => QueueSnapshot(snapshot)).CallDeferred();
			});
		}

		EnterPlay();
	}


	private void QueueSnapshot(byte[] snapshot)
	{
		if (Phase != MatchPhase.Playing || !NetworkManager.IsHost)
		{
			return;
		}

		OutgoingTotal = (snapshot.Length + CHUNK_SIZE - 1) / CHUNK_SIZE;
		OutgoingIndex = 0;

		for (int offset = 0; offset < snapshot.Length; offset += CHUNK_SIZE)
		{
			int size = Math.Min(CHUNK_SIZE, snapshot.Length - offset);
			byte[] chunk = new byte[size];

			Buffer.BlockCopy(snapshot, offset, chunk, 0, size);

			Outgoing.Enqueue(chunk);
		}

		GD.Print($"[Match] snapshot {snapshot.Length / 1024} KB in {OutgoingTotal} chunks");
	}


	private void PumpChunks()
	{
		if (Outgoing.Count == 0 || !NetworkManager.IsHost || NetworkManager.PeerCount == 0)
		{
			return;
		}

		for (int sent = 0; sent < CHUNKS_PER_FRAME && Outgoing.Count > 0; sent++)
		{
			Rpc(MethodName.WorldChunk, OutgoingIndex, OutgoingTotal, Outgoing.Dequeue());

			OutgoingIndex++;
		}
	}

	#endregion


	#region Guest

	[Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void BeginMatch(byte[] configuration)
	{
		if (NetworkManager.IsHost)
		{
			return;
		}

		Reset();

		PendingConfiguration = MatchCodec.DecodeConfiguration(configuration);

		if (PendingConfiguration == null)
		{
			GD.PushWarning("Match: malformed configuration packet");
			return;
		}

		GD.Print($"[Match] joining, seed {PendingConfiguration.Seed}, side {PendingConfiguration.PointCountPerWorldSide}");

		SetPhase(MatchPhase.Receiving);
	}


	[Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void WorldChunk(int index, int total, byte[] data)
	{
		if (NetworkManager.IsHost || Phase != MatchPhase.Receiving || PendingConfiguration == null)
		{
			return;
		}

		if (index != Incoming.Count)
		{
			GD.PushWarning($"Match: chunk {index} out of order, expected {Incoming.Count}");
			return;
		}

		IncomingTotal = total;

		Incoming.Add(data);

		ProgressChanged?.Invoke(total > 0 ? Incoming.Count / (float)total : 0f);

		if (Incoming.Count < total)
		{
			return;
		}

		AssembleWorld();
	}


	private void AssembleWorld()
	{
		int size = 0;

		foreach (byte[] chunk in Incoming)
		{
			size += chunk.Length;
		}

		byte[] snapshot = new byte[size];
		int offset = 0;

		foreach (byte[] chunk in Incoming)
		{
			Buffer.BlockCopy(chunk, 0, snapshot, offset, chunk.Length);

			offset += chunk.Length;
		}

		Incoming.Clear();

		WorldLayers layers = MatchCodec.DecodeWorld(snapshot, PendingConfiguration);

		if (layers == null)
		{
			GD.PushWarning("Match: malformed world snapshot");

			SetPhase(MatchPhase.Idle);
			return;
		}

		State = new MatchState(PendingConfiguration, layers);

		GD.Print($"[Match] world received, {IncomingTotal} chunks");

		EnterPlay();
	}

	#endregion


	#region Lifecycle

	private void EnterPlay()
	{
		SetPhase(MatchPhase.Playing);

		GetTree().ChangeSceneToFile(MATCH_SCENE);
	}


	public void Leave()
	{
		if (Phase == MatchPhase.Idle)
		{
			return;
		}

		Reset();

		SetPhase(MatchPhase.Idle);

		SessionManager.Instance?.Leave();

		GetTree().ChangeSceneToFile(MENU_SCENE);
	}


	private void Reset()
	{
		State = null;
		PendingConfiguration = null;
		Generated = null;

		Outgoing.Clear();
		Incoming.Clear();

		OutgoingTotal = 0;
		OutgoingIndex = 0;
		IncomingTotal = 0;
	}


	private void OnNetworkClosed()
	{
		if (Phase == MatchPhase.Receiving || Phase == MatchPhase.Generating)
		{
			Reset();

			SetPhase(MatchPhase.Idle);
		}
	}


	private void SetPhase(MatchPhase phase)
	{
		if (Phase == phase)
		{
			return;
		}

		Phase = phase;

		PhaseChanged?.Invoke();
	}

	#endregion
}
