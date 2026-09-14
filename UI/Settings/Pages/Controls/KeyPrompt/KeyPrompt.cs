using Godot;
using System;

public partial class KeyPrompt : Control
{
	#region Constants

	private const string HINT = "Escape to cancel";

	#endregion


	#region Events

	public event Action<Key> Captured;

	#endregion


	#region Nodes

	private Label ActionLabel;
	private Label HintLabel;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		ActionLabel = GetNode<Label>("%ActionLabel");
		HintLabel = GetNode<Label>("%HintLabel");
	}


	public override void _Input(InputEvent input)
	{
		if (!Visible || input is not InputEventKey key || !key.Pressed || key.Echo)
		{
			return;
		}

		GetViewport().SetInputAsHandled();

		Captured?.Invoke(key.PhysicalKeycode);
	}

	#endregion


	#region Content

	public void Open(string action)
	{
		ActionLabel.Text = action.ToUpperInvariant();
		HintLabel.Text = HINT;

		Visible = true;
	}


	public void Reject(string reason)
	{
		HintLabel.Text = reason;
	}

	#endregion
}
