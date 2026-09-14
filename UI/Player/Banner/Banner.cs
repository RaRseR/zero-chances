using Godot;

public partial class Banner : PanelContainer
{
	#region Constants

	private const float LOCKED_ALPHA = 0.45f;

	#endregion


	#region Nodes

	private TextureRect Avatar;
	private Label NameLabel;
	private HBoxContainer Actions;
	private Button AccessButton;
	private Button KickButton;

	#endregion


	#region State

	private Player CurrentPlayer;

	private bool ActionsVisible = true;

	#endregion


	#region Exports

	[Export]
	public bool ShowActions
	{
		get => ActionsVisible;
		set
		{
			ActionsVisible = value;

			if (Actions != null)
			{
				Actions.Visible = value;
			}
		}
	}

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		Avatar = GetNode<TextureRect>("%Avatar");
		NameLabel = GetNode<Label>("%NameLabel");
		Actions = GetNode<HBoxContainer>("%Actions");
		AccessButton = GetNode<Button>("%AccessButton");
		KickButton = GetNode<Button>("%KickButton");

		Actions.Visible = ActionsVisible;

		AccessButton.Pressed += OnAccessPressed;
		KickButton.Pressed += OnKickPressed;

		SteamManager.AvatarLoaded += OnAvatarLoaded;

		Refresh();
	}


	public override void _ExitTree()
	{
		SteamManager.AvatarLoaded -= OnAvatarLoaded;
	}

	#endregion


	#region Content

	public void SetPlayer(Player player)
	{
		CurrentPlayer = player;

		Refresh();
	}


	public void SetColor(Color background, Color text)
	{
		if (NameLabel == null)
		{
			return;
		}

		if (GetThemeStylebox("panel").Duplicate() is StyleBoxFlat panel)
		{
			panel.BgColor = background;
			panel.BorderColor = Factions.Shade(background);

			AddThemeStyleboxOverride("panel", panel);
		}

		NameLabel.AddThemeColorOverride("font_color", text);

		AccessButton.AddThemeColorOverride("font_color", text);
		AccessButton.AddThemeColorOverride("font_hover_color", text);
		AccessButton.AddThemeColorOverride("font_pressed_color", text);
		AccessButton.AddThemeColorOverride("font_hover_pressed_color", text);

		KickButton.AddThemeColorOverride("icon_normal_color", text);
	}


	private void Refresh()
	{
		if (CurrentPlayer == null || NameLabel == null)
		{
			return;
		}

		NameLabel.Text = BuildName();

		if (CurrentPlayer.FactionIndex >= 0)
		{
			SetColor(CurrentPlayer.Faction.Base, CurrentPlayer.Faction.Text);
		}

		SessionManager session = SessionManager.Instance;

		KickButton.Visible = session != null && session.CanKick(CurrentPlayer);

		AccessButton.Visible = session != null && session.CanGrantWorld(CurrentPlayer);
		AccessButton.SetPressedNoSignal(CurrentPlayer.CanEditWorld);
		AccessButton.Modulate = new Color(1f, 1f, 1f, CurrentPlayer.CanEditWorld ? 1f : LOCKED_ALPHA);

		RefreshAvatar();
	}


	private string BuildName()
	{
		if (CurrentPlayer.IsBot)
		{
			return $"{CurrentPlayer.Name} (bot)";
		}

		return CurrentPlayer.IsHost ? $"{CurrentPlayer.Name} (host)" : CurrentPlayer.Name;
	}


	private void RefreshAvatar()
	{
		Avatar.Texture = CurrentPlayer == null || CurrentPlayer.IsBot
			? null
			: SteamManager.GetAvatar(CurrentPlayer.SteamId);
	}


	private void OnAvatarLoaded(ulong steamId)
	{
		if (CurrentPlayer != null && CurrentPlayer.SteamId == steamId)
		{
			RefreshAvatar();
		}
	}

	#endregion


	#region Actions

	private void OnAccessPressed()
	{
		if (CurrentPlayer != null)
		{
			SessionManager.Instance?.ToggleWorldAccess(CurrentPlayer);
		}
	}


	private void OnKickPressed()
	{
		if (CurrentPlayer != null)
		{
			SessionManager.Instance?.Kick(CurrentPlayer);
		}
	}

	#endregion
}
