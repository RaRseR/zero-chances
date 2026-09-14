using Godot;

public partial class VideoPage : SettingsSection
{
	#region Constants

	private const string ACTIVE = "SegmentActive";
	private const string IDLE = "Segment";

	private static readonly string[] MODES = { "WINDOW", "BORDERLESS", "FULL" };

	#endregion


	#region Nodes

	private OptionButton ResolutionOption;
	private Label ResolutionHint;

	private readonly Button[] ModeButtons = new Button[MODES.Length];

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		ResolutionOption = GetNode<OptionButton>("%ResolutionOption");
		ResolutionHint = GetNode<Label>("%ResolutionHint");

		HBoxContainer bar = GetNode<HBoxContainer>("%Modes");

		for (int index = 0; index < MODES.Length; index++)
		{
			int captured = index;

			ModeButtons[index] = bar.GetNode<Button>(MODES[index]);
			ModeButtons[index].Pressed += () => OnModePressed(captured);
		}

		BuildResolutions();

		ResolutionOption.ItemSelected += OnResolutionSelected;

		Refresh();
	}

	#endregion


	#region Content

	private void BuildResolutions()
	{
		ResolutionOption.Clear();

		for (int index = 0; index < SettingsManager.RESOLUTIONS.Length; index++)
		{
			Vector2I size = SettingsManager.RESOLUTIONS[index];

			ResolutionOption.AddItem($"{size.X} x {size.Y}", index);
		}
	}


	public override void Refresh()
	{
		if (Config == null)
		{
			return;
		}

		Vector2I resolution = Config.Get(SettingsManager.VIDEO_RESOLUTION).AsVector2I();

		int selected = System.Array.IndexOf(SettingsManager.RESOLUTIONS, resolution);

		ResolutionOption.Selected = selected;

		int mode = Config.Get(SettingsManager.VIDEO_SCREEN_MODE).AsInt32();

		for (int index = 0; index < ModeButtons.Length; index++)
		{
			ModeButtons[index].ThemeTypeVariation = index == mode ? ACTIVE : IDLE;
		}

		bool windowed = mode == 0;

		ResolutionOption.Disabled = !windowed;
		ResolutionHint.Text = windowed ? "WINDOWED" : "NATIVE";
	}

	#endregion


	#region Actions

	private void OnResolutionSelected(long index)
	{
		if (index < 0 || index >= SettingsManager.RESOLUTIONS.Length)
		{
			return;
		}

		Config?.Set(SettingsManager.VIDEO_RESOLUTION, SettingsManager.RESOLUTIONS[index]);

		Refresh();
	}


	private void OnModePressed(int mode)
	{
		Config?.Set(SettingsManager.VIDEO_SCREEN_MODE, mode);

		Refresh();
	}

	#endregion
}
