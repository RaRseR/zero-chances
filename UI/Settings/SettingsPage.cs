using Godot;
using System.Collections.Generic;

public partial class SettingsPage : Control
{
	#region Constants

	private const string PAGE_PATH = "res://UI/Settings/Pages/{0}/{0}Page.tscn";

	private const string ACTIVE = "PrimaryButton";

	private static readonly string[] SECTIONS =
	{
		"Video", "Audio", "Controls", "Gameplay"
	};

	#endregion


	#region Nodes

	private PanelContainer PageHost;

	#endregion


	#region State

	private readonly Dictionary<string, Button> Buttons = new();

	private SettingsSection CurrentPage;

	private string CurrentSection = string.Empty;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		PageHost = GetNode<PanelContainer>("%PageHost");

		foreach (string section in SECTIONS)
		{
			string captured = section;

			Button button = GetNode<Button>($"%{section}Button");

			button.Pressed += () => Open(captured);

			Buttons[section] = button;
		}

		GetNode<Button>("%BackButton").Pressed += OnBackPressed;
		GetNode<Button>("%ResetButton").Pressed += OnResetPressed;
		GetNode<Button>("%ApplyButton").Pressed += OnApplyPressed;

		Open(SECTIONS[0]);
	}

	#endregion


	#region Sections

	private void Open(string section)
	{
		CurrentSection = section;

		foreach (KeyValuePair<string, Button> pair in Buttons)
		{
			pair.Value.ThemeTypeVariation = pair.Key == section ? ACTIVE : string.Empty;
		}

		if (CurrentPage != null)
		{
			PageHost.RemoveChild(CurrentPage);

			CurrentPage.QueueFree();
			CurrentPage = null;
		}

		PackedScene scene = GD.Load<PackedScene>(string.Format(PAGE_PATH, section));

		if (scene == null)
		{
			GD.PushWarning($"SettingsPage: no scene for section {section}");
			return;
		}

		CurrentPage = scene.Instantiate<SettingsSection>();

		PageHost.AddChild(CurrentPage);
	}

	#endregion


	#region Actions

	private void OnApplyPressed()
	{
		SettingsManager.Instance?.Save();
	}


	private void OnResetPressed()
	{
		SettingsManager.Instance?.ResetSection(CurrentSection.ToLowerInvariant());

		CurrentPage?.Refresh();
	}


	private void OnBackPressed()
	{
		SettingsManager.Instance?.Reload();

		MenuRoot menu = MenuRoot.FindIn(this);

		if (menu != null && menu.CanGoBack)
		{
			menu.Back();
			return;
		}

		QueueFree();
	}

	#endregion
}
