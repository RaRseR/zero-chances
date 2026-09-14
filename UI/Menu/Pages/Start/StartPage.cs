using Godot;

public partial class StartPage : Control
{
	#region Constants

	private const string PLAY_PAGE = "res://UI/Menu/Pages/Play/PlayPage.tscn";
	private const string SETTINGS_PAGE = "res://UI/Settings/SettingsPage.tscn";
	private const string GENERATOR_PAGE = "res://UI/Menu/Pages/Generator/GeneratorPage.tscn";

	#endregion


	#region Exports

	[Export] public float VeilAlpha { get; set; } = 0.55f;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		GetNode<Button>("%PlayButton").Pressed += OnPlayPressed;
		GetNode<Button>("%SettingsButton").Pressed += OnSettingsPressed;
		GetNode<Button>("%GeneratorButton").Pressed += OnGeneratorPressed;
		GetNode<Button>("%ExitButton").Pressed += OnExitPressed;

		GetNode<Banner>("%Banner").SetPlayer(new Player
		{
			SteamId = NetworkManager.LocalIdentity,
			Name = NetworkManager.LocalName
		});
	}

	#endregion


	#region Actions

	private void OnPlayPressed()
	{
		MenuRoot.FindIn(this)?.Open(PLAY_PAGE);
	}


	private void OnSettingsPressed()
	{
		MenuRoot.FindIn(this)?.Open(SETTINGS_PAGE);
	}


	private void OnGeneratorPressed()
	{
		MenuRoot.FindIn(this)?.Open(GENERATOR_PAGE);
	}


	private void OnExitPressed()
	{
		GetTree().Quit();
	}

	#endregion
}
