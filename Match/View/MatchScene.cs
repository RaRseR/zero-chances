using Godot;

public partial class MatchScene : Node3D
{
	#region Nodes

	private World WorldInstance;
	private Camera CameraInstance;
	private Label StatusLabel;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		WorldInstance = GetNode<World>("%World");
		CameraInstance = GetNode<Camera>("%Camera");
		StatusLabel = GetNode<Label>("%StatusLabel");

		MatchState state = MatchManager.Instance?.State;

		if (state == null)
		{
			GD.PushWarning("MatchScene: no match state, returning to menu");

			MatchManager.Instance?.Leave();
			return;
		}

		ApplyViewportSize();

		GetViewport().SizeChanged += ApplyViewportSize;

		WorldInstance.Build(state.Layers, state.Configuration, CameraInstance);

		StatusLabel.Text =
			$"turn {state.Turn}   seed {state.Configuration.Seed}   " +
			$"{state.Configuration.PointCountPerWorldSide} points   [Esc] leave";
	}


	public override void _ExitTree()
	{
		GetViewport().SizeChanged -= ApplyViewportSize;
	}


	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventKey key && key.Pressed && key.Keycode == Key.Escape)
		{
			GetViewport().SetInputAsHandled();

			MatchManager.Instance?.Leave();
		}
	}

	#endregion


	#region Viewport

	private void ApplyViewportSize()
	{
		Vector2 size = GetViewport().GetVisibleRect().Size;

		CameraInstance.SetViewportSize(new Vector2I((int)size.X, (int)size.Y));
	}

	#endregion
}
