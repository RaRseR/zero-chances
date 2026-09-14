using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class LobbyPage : Control
{
	#region Constants

	private const string BANNER = "res://UI/Player/Banner/Banner.tscn";
	private const string FACTION_SELECTOR = "res://UI/Menu/Pages/Lobby/FactionSelector/FactionSelector.tscn";
	private const string FEATURE_CARD = "res://UI/Card/Card.tscn";
	private const string FEATURE_CARD_ART = "res://Assets/Dev/2.jpg";
	private const string EMPTY_SLOT = "res://UI/Menu/Pages/Lobby/EmptySlot/EmptySlot.tscn";

	private static readonly Color EXCLUDED = new Color(1f, 1f, 1f, 0.4f);

	private const string SIZE_CATEGORY = "SIZE";
	private const string SHAPE_CATEGORY = "SHAPE";
	private const string LANDFORM_CATEGORY = "LANDFORM";
	private const string CLIMATE_CATEGORY = "CLIMATE";

	#endregion


	#region Nodes

	private LineEdit NameEdit;
	private LineEdit SeedEdit;
	private LineEdit PasswordEdit;
	private Control SeedField;
	private Control PasswordField;
	private Label PlayersHeader;
	private GridContainer PlayerHost;
	private GridContainer FeatureHost;
	private HBoxContainer ChosenFeatureHost;
	private Button StartButton;
	private Button AddBotButton;
	private Button FillBotsButton;
	private Button RemoveBotsButton;
	private Button RollButton;
	private Button InviteButton;

	private Button FactionButton;

	private Button OpenButton;
	private Button FriendsButton;
	private Button PassButton;

	private HBoxContainer FilterHost;

	#endregion


	#region State

	private bool IsHost = true;

	private readonly Dictionary<Type, WorldFeature> InstanceByType = new();
	private readonly Dictionary<Type, Card> FeatureCardByType = new();
	private readonly Dictionary<string, Button> FilterByCategory = new();
	private readonly Dictionary<Type, string> CategoryByType = new();

	private readonly List<Type> Chosen = new();

	private WorldSizeFeature SelectedSize;

	private string Filter = string.Empty;

	private readonly List<Player> Players = new();

	private PackedScene BannerScene;
	private PackedScene SlotScene;
	private FactionSelector Picker;
	private Player PickerPlayer;

	#endregion


	#region Setup

	public void Configure(bool isHost)
	{
		IsHost = isHost;
	}

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		NameEdit = GetNode<LineEdit>("%NameEdit");
		SeedEdit = GetNode<LineEdit>("%SeedEdit");
		PasswordEdit = GetNode<LineEdit>("%PasswordEdit");
		SeedField = GetNode<Control>("%SeedField");
		PasswordField = GetNode<Control>("%PasswordField");
		PlayersHeader = GetNode<Label>("%PlayersHeader");
		PlayerHost = GetNode<GridContainer>("%PlayerHost");
		FeatureHost = GetNode<GridContainer>("%FeatureHost");
		ChosenFeatureHost = GetNode<HBoxContainer>("%ChosenFeatureHost");
		StartButton = GetNode<Button>("%StartButton");
		AddBotButton = GetNode<Button>("%AddBotButton");
		FillBotsButton = GetNode<Button>("%FillBotsButton");
		RemoveBotsButton = GetNode<Button>("%RemoveBotsButton");
		RollButton = GetNode<Button>("%RollButton");
		InviteButton = GetNode<Button>("%InviteButton");

		FactionButton = GetNode<Button>("%FactionButton");

		OpenButton = GetNode<Button>("%OpenButton");
		FriendsButton = GetNode<Button>("%FriendsButton");
		PassButton = GetNode<Button>("%PassButton");

		FilterHost = GetNode<HBoxContainer>("%Filters");

		BannerScene = GD.Load<PackedScene>(BANNER);
		SlotScene = GD.Load<PackedScene>(EMPTY_SLOT);

		Picker = GD.Load<PackedScene>(FACTION_SELECTOR).Instantiate<FactionSelector>();
		Picker.Chosen += OnFactionChosen;

		AddChild(Picker);

		BuildCards();

		ButtonGroup access = new ButtonGroup();

		OpenButton.ButtonGroup = access;
		FriendsButton.ButtonGroup = access;
		PassButton.ButtonGroup = access;

		OpenButton.Pressed += () => SetAccess(OpenButton);
		FriendsButton.Pressed += () => SetAccess(FriendsButton);
		PassButton.Pressed += () => SetAccess(PassButton);


		NameEdit.Text = $"{NetworkManager.LocalName}'s Lobby";
		NameEdit.TextChanged += _ => Publish();
		PasswordEdit.TextChanged += _ => Publish();

		RollButton.Pressed += OnRollPressed;

		FactionButton.Pressed += OnFactionPressed;

		StartButton.Pressed += OnStartPressed;
		AddBotButton.Pressed += OnAddBotPressed;
		FillBotsButton.Pressed += OnFillBotsPressed;
		RemoveBotsButton.Pressed += OnRemoveBotsPressed;
		InviteButton.Pressed += OnInvitePressed;

		GetNode<Button>("%LeaveButton").Pressed += OnLeavePressed;

		StartButton.Visible = IsHost;
		AddBotButton.Visible = IsHost;
		FillBotsButton.Visible = IsHost;
		RemoveBotsButton.Visible = IsHost;
		InviteButton.Visible = IsHost;

		NameEdit.Editable = IsHost;

		SeedField.Visible = IsHost;
		PasswordField.Visible = IsHost;

		OpenButton.Disabled = !IsHost;
		FriendsButton.Disabled = !IsHost;
		PassButton.Disabled = !IsHost;

		SetAccess(OpenButton);

		SessionManager.RosterChanged += OnRosterChanged;
		NetworkManager.Failed += OnNetworkFailed;

		Publish();

		if (IsHost && !NetworkManager.IsActive)
		{
			SessionManager.Instance?.BeginHost(NameEdit.Text, MaxPlayersValue, PasswordEdit.Text);
		}

		OnRosterChanged();
	}


	public override void _ExitTree()
	{
		SessionManager.RosterChanged -= OnRosterChanged;
		NetworkManager.Failed -= OnNetworkFailed;
	}

	#endregion


	#region Settings

	private int MaxPlayersValue => IsHost
		? SelectedSize?.MaxFactionCount ?? 0
		: SessionManager.Instance?.MaxPlayers ?? 0;


	private void SetAccess(Button chosen)
	{
		OpenButton.ThemeTypeVariation = chosen == OpenButton ? "SegmentActive" : "Segment";
		FriendsButton.ThemeTypeVariation = chosen == FriendsButton ? "SegmentActive" : "Segment";
		PassButton.ThemeTypeVariation = chosen == PassButton ? "SegmentActive" : "Segment";

		bool locked = chosen == PassButton;

		PasswordEdit.Editable = IsHost && locked;

		if (!locked)
		{
			PasswordEdit.Text = string.Empty;
		}

		Publish();
	}


	private void Publish()
	{
		if (IsHost)
		{
			SessionManager.Instance?.UpdateSettings(NameEdit.Text, MaxPlayersValue, PasswordEdit.Text);
		}
	}

	#endregion


	#region World cards

	private void BuildCards()
	{
		BuildFilters();

		List<Type> sizes = CollectTypes(typeof(WorldSizeFeature));
		List<Type> shapes = CollectTypes(typeof(WorldShapeFeature));
		List<Type> landforms = CollectTypes(typeof(WorldLandformFeature));
		List<Type> climates = CollectTypes(typeof(WorldClimateFeature));

		PackedScene scene = GD.Load<PackedScene>(FEATURE_CARD);
		Texture2D art = GD.Load<Texture2D>(FEATURE_CARD_ART);

		foreach (Type type in sizes)
		{
			WorldSizeFeature size = (WorldSizeFeature)InstanceByType[type];

			MakeFeatureCard(scene, art, type, SIZE_CATEGORY,
				$"{size.ChunkCountPerWorldSide} CHUNKS · {size.MaxFactionCount} PLAYERS");
		}

		foreach (Type type in shapes)
		{
			MakeFeatureCard(scene, art, type, SHAPE_CATEGORY, string.Empty);
		}

		foreach (Type type in landforms)
		{
			MakeFeatureCard(scene, art, type, LANDFORM_CATEGORY, string.Empty);
		}

		foreach (Type type in climates)
		{
			int minimum = InstanceByType[type].MinimumChunkCount;

			MakeFeatureCard(scene, art, type, CLIMATE_CATEGORY, minimum > 0 ? "STANDARD OR LARGER" : string.Empty);
		}

		if (sizes.Count > 0)
		{
			Chosen.Add(sizes[0]);

			SelectedSize = (WorldSizeFeature)InstanceByType[sizes[0]];
		}

		EnsureDefault(typeof(WorldShapeFeature), typeof(ContinentFeature));
		EnsureDefault(typeof(WorldClimateFeature), typeof(TemperateClimateFeature));

		RefreshWorld();
	}


	private void MakeFeatureCard(PackedScene scene, Texture2D art, Type type, string category, string stat)
	{
		Card card = scene.Instantiate<Card>();

		card.CustomMinimumSize = new Vector2(160, 230);

		card.Title = BuildTitle(type);
		card.Type = category;
		card.Rules = stat;
		card.Art = art;
		card.Rarity = string.Empty;
		card.ShowCost = false;
		card.ShowStats = false;

		card.Pressed += () => OnCardPressed(type);

		FeatureCardByType[type] = card;
		CategoryByType[type] = category;
	}


	private static string BuildTitle(Type type)
	{
		string name = type.Name.Replace("Feature", string.Empty).Replace("WorldShape", string.Empty);

		if (name.EndsWith("Size"))
		{
			name = name[..^4];
		}

		if (name.EndsWith("Climate"))
		{
			name = name[..^7];
		}

		return System.Text.RegularExpressions.Regex.Replace(name, "(?<=[a-z])(?=[A-Z])", " ");
	}


	private void OnCardPressed(Type type)
	{
		if (SessionManager.Instance == null || !SessionManager.Instance.CanEditWorld)
		{
			return;
		}

		if (Chosen.Contains(type))
		{
			Release(type);
			return;
		}

		Choose(type);
	}


	private void Choose(Type type)
	{
		WorldFeature feature = InstanceByType[type];

		if (feature is WorldSizeFeature size)
		{
			Chosen.RemoveAll(chosen => InstanceByType[chosen] is WorldSizeFeature);

			Chosen.Insert(0, type);

			SelectedSize = size;

			DropUnfitting();

			Publish();
			RefreshPlayers();
			RefreshWorld();
			return;
		}

		if (FeatureRules.IsSingleChoice(feature))
		{
			Type group = FeatureRules.GroupOf(feature);

			Chosen.RemoveAll(chosen => FeatureRules.GroupOf(InstanceByType[chosen]) == group);

			Chosen.Add(type);

			RefreshWorld();
			return;
		}

		Chosen.RemoveAll(chosen => FeatureRules.Conflicts(InstanceByType[chosen], feature));
		Chosen.Add(type);

		RefreshWorld();
	}


	private void DropUnfitting()
	{
		Chosen.RemoveAll(chosen => !FeatureRules.FitsSize(InstanceByType[chosen], SelectedSize));

		EnsureDefault(typeof(WorldShapeFeature), typeof(ContinentFeature));
		EnsureDefault(typeof(WorldClimateFeature), typeof(TemperateClimateFeature));
	}


	private void EnsureDefault(Type group, Type fallback)
	{
		if (Chosen.Any(chosen => group.IsAssignableFrom(chosen)))
		{
			return;
		}

		if (InstanceByType.ContainsKey(fallback))
		{
			Chosen.Add(fallback);
		}
	}


	private void Release(Type type)
	{
		if (FeatureRules.IsSingleChoice(InstanceByType[type]))
		{
			return;
		}

		Chosen.Remove(type);

		RefreshWorld();
	}



	private void SetFilter(string category)
	{
		Filter = category;

		foreach (KeyValuePair<string, Button> pair in FilterByCategory)
		{
			bool active = pair.Key == category;

			pair.Value.ThemeTypeVariation = active ? "SegmentActive" : "Segment";
			pair.Value.ButtonPressed = active;
		}

		RefreshWorld();
	}


	private void BuildFilters()
	{
		foreach (Node child in FilterHost.GetChildren())
		{
			FilterHost.RemoveChild(child);
			child.QueueFree();
		}

		FilterByCategory.Clear();

		AddFilter(string.Empty, "ALL");
		AddFilter(SIZE_CATEGORY, SIZE_CATEGORY);
		AddFilter(SHAPE_CATEGORY, SHAPE_CATEGORY);
		AddFilter(LANDFORM_CATEGORY, LANDFORM_CATEGORY);
		AddFilter(CLIMATE_CATEGORY, CLIMATE_CATEGORY);
	}


	private void AddFilter(string category, string label)
	{
		Button button = new Button();

		bool active = category == Filter;

		button.Text = label;
		button.FocusMode = FocusModeEnum.None;
		button.ToggleMode = true;
		button.ThemeTypeVariation = active ? "SegmentActive" : "Segment";
		button.ButtonPressed = active;
		button.Pressed += () => SetFilter(category);

		FilterHost.AddChild(button);

		FilterByCategory[category] = button;
	}


	private void RefreshWorld()
	{
		Detach(ChosenFeatureHost);
		Detach(FeatureHost);

		foreach (Type type in Chosen)
		{
			ChosenFeatureHost.AddChild(FeatureCardByType[type]);
		}

		foreach (KeyValuePair<Type, Card> pair in FeatureCardByType)
		{
			if (Chosen.Contains(pair.Key))
			{
				continue;
			}

			FeatureHost.AddChild(pair.Value);

			pair.Value.Visible = Filter.Length == 0 || CategoryByType[pair.Key] == Filter;
		}

		RefreshWorldAccess();
	}


	private static void Detach(Node host)
	{
		foreach (Node child in host.GetChildren())
		{
			host.RemoveChild(child);
		}
	}


	private List<Type> CollectTypes(Type category)
	{
		List<Type> types = category.Assembly.GetTypes()
			.Where(type => category.IsAssignableFrom(type) && !type.IsAbstract)
			.ToList();

		foreach (Type type in types)
		{
			InstanceByType[type] = (WorldFeature)Activator.CreateInstance(type);
		}

		if (types.Count > 0 && InstanceByType[types[0]] is WorldSizeFeature)
		{
			return types
				.OrderBy(type => ((WorldSizeFeature)InstanceByType[type]).ChunkCountPerWorldSide)
				.ToList();
		}

		return types.OrderBy(type => type.Name).ToList();
	}

	#endregion


	#region Players

	private void OnRosterChanged()
	{
		Players.Clear();

		IReadOnlyList<Player> roster = SessionManager.Instance?.Players;

		if (roster != null)
		{
			Players.AddRange(roster);
		}

		RefreshPlayers();
	}


	private void RefreshPlayers()
	{
		foreach (Node child in PlayerHost.GetChildren())
		{
			PlayerHost.RemoveChild(child);
			child.QueueFree();
		}

		foreach (Player player in Players)
		{
			BuildRow(player);
		}

		for (int index = Players.Count; index < MaxPlayersValue; index++)
		{
			BuildSlot(index + 1);
		}

		bool canAdd = SessionManager.Instance != null && SessionManager.Instance.CanAddBot();
		bool canRemove = SessionManager.Instance != null && SessionManager.Instance.CanRemoveBots();

		PlayersHeader.Text = $"Players {Players.Count} / {MaxPlayersValue}";

		RefreshFaction();

		AddBotButton.Disabled = !canAdd;
		FillBotsButton.Disabled = !canAdd;
		RemoveBotsButton.Disabled = !canRemove;

		RefreshWorldAccess();
	}


	private void RefreshWorldAccess()
	{
		SessionManager session = SessionManager.Instance;

		bool editable = session != null && session.CanEditWorld;

		foreach (KeyValuePair<Type, Card> pair in FeatureCardByType)
		{
			WorldFeature feature = InstanceByType[pair.Key];

			if (feature is WorldSizeFeature size)
			{
				bool fits = session != null && session.CanUseSize(size.MaxFactionCount);

				pair.Value.Disabled = !editable || !fits;
				pair.Value.Modulate = fits ? Colors.White : EXCLUDED;
				pair.Value.TooltipText = fits ? string.Empty : "Too many players for this world size";

				continue;
			}

			bool allowed = FeatureRules.FitsSize(feature, SelectedSize);

			bool blocked = !allowed || (!FeatureRules.IsSingleChoice(feature)
				&& !Chosen.Contains(pair.Key)
				&& Chosen.Any(other => FeatureRules.Conflicts(InstanceByType[other], feature)));

			pair.Value.Disabled = !editable || blocked;
			pair.Value.Modulate = blocked ? EXCLUDED : Colors.White;
			pair.Value.TooltipText = allowed ? string.Empty : "Needs a larger world";
		}
	}


	private void BuildRow(Player player)
	{
		Banner banner = BannerScene.Instantiate<Banner>();

		PlayerHost.AddChild(banner);

		banner.SetPlayer(player);
	}


	private void BuildSlot(int number)
	{
		EmptySlot slot = SlotScene.Instantiate<EmptySlot>();

		PlayerHost.AddChild(slot);

		slot.Setup(number);
	}


	private Player LocalPlayer => SessionManager.Instance?.LocalPlayer;


	private void RefreshFaction()
	{
		Player local = LocalPlayer;

		FactionButton.Disabled = local == null;

		if (local == null)
		{
			return;
		}

		Faction faction = local.Faction;

		PaintSwatch("normal", faction);
		PaintSwatch("hover", faction);
		PaintSwatch("hover_pressed", faction);
		PaintSwatch("pressed", faction);
		PaintSwatch("disabled", faction);
	}


	private void PaintSwatch(string state, Faction faction)
	{
		if (FactionButton.GetThemeStylebox(state).Duplicate() is not StyleBoxFlat swatch)
		{
			return;
		}

		swatch.BgColor = faction.Base;
		swatch.BorderColor = Factions.Shade(faction.Base);

		FactionButton.AddThemeStyleboxOverride(state, swatch);
	}


	private void OnFactionPressed()
	{
		Player local = LocalPlayer;

		if (local == null)
		{
			return;
		}

		PickerPlayer = local;

		Picker.Open(Players, local);
	}


	private void OnFactionChosen(int index)
	{
		if (PickerPlayer != null)
		{
			SessionManager.Instance?.ChangeFaction(PickerPlayer, index);
		}
	}

	#endregion


	#region Actions

	private void OnAddBotPressed()
	{
		SessionManager.Instance?.AddBot();
	}


	private void OnFillBotsPressed()
	{
		SessionManager.Instance?.FillWithBots();
	}


	private void OnRemoveBotsPressed()
	{
		SessionManager.Instance?.RemoveBots();
	}


	private void OnInvitePressed()
	{
		SteamManager.Instance?.InviteFriend();
	}


	private void OnStartPressed()
	{
		List<WorldFeature> selected = new();

		foreach (Type type in Chosen)
		{
			selected.Add(InstanceByType[type]);
		}

		SessionConfiguration configuration = SessionConfiguration.FromFeatures(
			ResolveSeed(),
			selected,
			Players
		);

		GD.Print($"Start: \"{NameEdit.Text}\", seed {configuration.Seed}, players {configuration.Players.Length}");

		if (MatchManager.Instance == null || !MatchManager.Instance.StartMatch(configuration))
		{
			GD.PushWarning("Lobby: could not start the match");
			return;
		}

		StartButton.Disabled = true;
	}


	private void OnRollPressed()
	{
		SeedEdit.Text = (GD.Randi() >> 1).ToString();
	}


	private int ResolveSeed()
	{
		string text = SeedEdit.Text.Trim();

		if (text.Length == 0)
		{
			return (int)GD.Randi();
		}

		if (int.TryParse(text, out int number))
		{
			return number;
		}

		return GD.Hash(text);
	}


	private void OnNetworkFailed(string reason)
	{
		GD.Print($"[Lobby] leaving: {reason}");

		Callable.From(ReturnToMenu).CallDeferred();
	}


	private void ReturnToMenu()
	{
		MenuRoot.FindIn(this)?.Back();
	}


	private void OnLeavePressed()
	{
		SessionManager.Instance?.Leave();

		ReturnToMenu();
	}

	#endregion
}
