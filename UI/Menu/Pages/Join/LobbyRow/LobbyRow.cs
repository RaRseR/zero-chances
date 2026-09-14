using Godot;
using System;

public partial class LobbyRow : VBoxContainer
{
	#region Events

	public event Action<ulong> Joined;

	#endregion


	#region Nodes

	private Label IndexLabel;
	private Label NameLabel;
	private Label PlayersLabel;
	private Label PingLabel;
	private Button JoinButton;

	#endregion


	#region State

	private ulong LobbyId;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		IndexLabel = GetNode<Label>("%IndexLabel");
		NameLabel = GetNode<Label>("%NameLabel");
		PlayersLabel = GetNode<Label>("%PlayersLabel");
		PingLabel = GetNode<Label>("%PingLabel");
		JoinButton = GetNode<Button>("%JoinButton");

		JoinButton.Pressed += OnJoinPressed;
	}

	#endregion


	#region Content

	public void Setup(int index, SteamLobbyInfo lobby)
	{
		LobbyId = lobby.Id;

		IndexLabel.Text = index.ToString();
		NameLabel.Text = lobby.Name;
		PlayersLabel.Text = $"{lobby.PlayerCount} / {lobby.MaxPlayers}";
		PingLabel.Text = lobby.Ping < 0 ? "-" : $"{lobby.Ping} ms";
	}

	#endregion


	#region Actions

	private void OnJoinPressed()
	{
		Joined?.Invoke(LobbyId);
	}

	#endregion
}
