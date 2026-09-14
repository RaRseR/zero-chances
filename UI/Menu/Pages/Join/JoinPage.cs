using Godot;
using System.Collections.Generic;

public partial class JoinPage : Control
{
	#region Constants

	private const string LOBBY_PAGE = "res://UI/Menu/Pages/Lobby/LobbyPage.tscn";
	private const string LOBBY_ROW = "res://UI/Menu/Pages/Join/LobbyRow/LobbyRow.tscn";

	private const int PAGE_SIZE = 25;

	#endregion


	#region Nodes

	private LineEdit NameEdit;
	private SpinBox TotalSpin;
	private SpinBox FreeSpin;
	private SpinBox PingSpin;
	private Label StatusLabel;
	private VBoxContainer RowHost;

	private Button PrevButton;
	private Button NextButton;
	private Label PageLabel;

	private ConfirmationDialog PasswordDialog;
	private LineEdit PasswordEdit;

	#endregion


	#region State

	private readonly List<SteamLobbyInfo> Lobbies = new();
	private readonly List<SteamLobbyInfo> Filtered = new();

	private PackedScene RowScene;

	private int CurrentPage;

	private SteamLobbyInfo PendingLobby;

	private int PageCount => Filtered.Count == 0 ? 1 : (Filtered.Count + PAGE_SIZE - 1) / PAGE_SIZE;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		NameEdit = GetNode<LineEdit>("%NameEdit");
		TotalSpin = GetNode<SpinBox>("%TotalSpin");
		FreeSpin = GetNode<SpinBox>("%FreeSpin");
		PingSpin = GetNode<SpinBox>("%PingSpin");
		StatusLabel = GetNode<Label>("%StatusLabel");
		RowHost = GetNode<VBoxContainer>("%RowHost");

		PrevButton = GetNode<Button>("%PrevButton");
		NextButton = GetNode<Button>("%NextButton");
		PageLabel = GetNode<Label>("%PageLabel");

		RowScene = GD.Load<PackedScene>(LOBBY_ROW);

		BuildPasswordDialog();

		NameEdit.TextChanged += _ => ApplyFilters();
		TotalSpin.ValueChanged += _ => ApplyFilters();
		FreeSpin.ValueChanged += _ => ApplyFilters();
		PingSpin.ValueChanged += _ => ApplyFilters();

		PrevButton.Pressed += () => TurnPage(-1);
		NextButton.Pressed += () => TurnPage(1);

		GetNode<Button>("%RefreshButton").Pressed += Search;
		GetNode<Button>("%BackButton").Pressed += OnBackPressed;

		SteamManager.LobbyListReceived += OnLobbyListReceived;

		NetworkManager.Failed += OnNetworkFailed;

		Search();
	}


	public override void _ExitTree()
	{
		SteamManager.LobbyListReceived -= OnLobbyListReceived;

		NetworkManager.Failed -= OnNetworkFailed;
	}

	#endregion


	#region Password

	private void BuildPasswordDialog()
	{
		PasswordEdit = new LineEdit();
		PasswordEdit.Secret = true;
		PasswordEdit.PlaceholderText = "Password";
		PasswordEdit.CustomMinimumSize = new Vector2(240, 0);

		PasswordDialog = new ConfirmationDialog();
		PasswordDialog.Title = "This lobby is protected";
		PasswordDialog.Confirmed += OnPasswordConfirmed;

		PasswordDialog.AddChild(PasswordEdit);
		AddChild(PasswordDialog);
	}


	private void OnPasswordRequired()
	{
		PasswordEdit.Text = string.Empty;

		PasswordDialog.PopupCentered();
		PasswordEdit.GrabFocus();
	}


	private void OnPasswordConfirmed()
	{
		Attempt(PendingLobby, PasswordEdit.Text);
	}


	private void OnNetworkFailed(string reason)
	{
		StatusLabel.Text = reason;

		if (PendingLobby.Id != 0UL && reason.Contains("password", System.StringComparison.OrdinalIgnoreCase))
		{
			OnPasswordRequired();
		}
	}

	#endregion


	#region Filters

	private SteamLobbyQuery BuildQuery()
	{
		return new SteamLobbyQuery
		{
			Name = NameEdit.Text,
			MinTotalSlots = (int)TotalSpin.Value,
			MinFreeSlots = (int)FreeSpin.Value,
			MaxPing = (int)PingSpin.Value
		};
	}


	private void ApplyFilters()
	{
		SteamLobbyQuery query = BuildQuery();

		Filtered.Clear();

		foreach (SteamLobbyInfo lobby in Lobbies)
		{
			if (query.Matches(lobby))
			{
				Filtered.Add(lobby);
			}
		}

		CurrentPage = 0;

		RefreshTable();
	}

	#endregion


	#region Search

	private void Search()
	{
		if (!SteamManager.IsReady)
		{
			Lobbies.Clear();
			Filtered.Clear();

			StatusLabel.Text = "STEAM IS NOT RUNNING";

			RefreshTable();
			return;
		}

		StatusLabel.Text = "SEARCHING...";

		SteamManager.Instance?.FindLobbies(BuildQuery());
	}


	private void OnLobbyListReceived(List<SteamLobbyInfo> lobbies)
	{
		Lobbies.Clear();
		Lobbies.AddRange(lobbies);

		ApplyFilters();
	}

	#endregion


	#region Pagination

	private void TurnPage(int delta)
	{
		int target = Mathf.Clamp(CurrentPage + delta, 0, PageCount - 1);

		if (target == CurrentPage)
		{
			return;
		}

		CurrentPage = target;

		RefreshTable();
	}

	#endregion


	#region Table

	private void RefreshTable()
	{
		foreach (Node child in RowHost.GetChildren())
		{
			RowHost.RemoveChild(child);
			child.QueueFree();
		}

		if (CurrentPage >= PageCount)
		{
			CurrentPage = PageCount - 1;
		}

		int first = CurrentPage * PAGE_SIZE;
		int last = Mathf.Min(first + PAGE_SIZE, Filtered.Count);

		for (int index = first; index < last; index++)
		{
			AddRow(index + 1, Filtered[index]);
		}

		PageLabel.Text = $"PAGE {CurrentPage + 1} / {PageCount}";

		PrevButton.Disabled = CurrentPage == 0;
		NextButton.Disabled = CurrentPage >= PageCount - 1;

		if (Filtered.Count == 0)
		{
			StatusLabel.Text = Lobbies.Count == 0
				? "NO LOBBIES FOUND"
				: "NO LOBBIES MATCH THE FILTERS";

			return;
		}

		StatusLabel.Text = $"{first + 1}-{last} OF {Filtered.Count} LOBBIES";
	}


	private void AddRow(int number, SteamLobbyInfo lobby)
	{
		LobbyRow row = RowScene.Instantiate<LobbyRow>();

		RowHost.AddChild(row);

		row.Setup(number, lobby);
		row.Joined += OnJoinPressed;
	}

	#endregion


	#region Actions

	private void OnJoinPressed(ulong lobbyId)
	{
		foreach (SteamLobbyInfo lobby in Filtered)
		{
			if (lobby.Id == lobbyId)
			{
				Attempt(lobby, string.Empty);
				return;
			}
		}
	}


	private void Attempt(SteamLobbyInfo lobby, string password)
	{
		PendingLobby = lobby;

		StatusLabel.Text = "CONNECTING...";

		if (SessionManager.Instance != null && SessionManager.Instance.BeginJoin(lobby, password))
		{
			MenuRoot.FindIn(this)?.Open(LOBBY_PAGE, page => ((LobbyPage)page).Configure(false));
		}
	}


	private void OnBackPressed()
	{
		MenuRoot.FindIn(this)?.Back();
	}

	#endregion
}
