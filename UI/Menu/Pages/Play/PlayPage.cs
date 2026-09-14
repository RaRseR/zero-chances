using Godot;

public partial class PlayPage : Control
{
	#region Constants

	private const string LOBBY_PAGE = "res://UI/Menu/Pages/Lobby/LobbyPage.tscn";
	private const string JOIN_PAGE = "res://UI/Menu/Pages/Join/JoinPage.tscn";

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		GetNode<Button>("%CreateCard").Pressed += OnCreatePressed;
		GetNode<Button>("%JoinCard").Pressed += OnJoinPressed;
		GetNode<Button>("%BackButton").Pressed += OnBackPressed;
	}

	#endregion


	#region Actions

	private void OnCreatePressed()
	{
		MenuRoot.FindIn(this)?.Open(LOBBY_PAGE, page => ((LobbyPage)page).Configure(true));
	}


	private void OnJoinPressed()
	{
		MenuRoot.FindIn(this)?.Open(JOIN_PAGE);
	}


	private void OnBackPressed()
	{
		MenuRoot.FindIn(this)?.Back();
	}

	#endregion
}
