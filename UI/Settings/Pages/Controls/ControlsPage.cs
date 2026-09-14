using Godot;
using System.Collections.Generic;

public partial class ControlsPage : SettingsSection
{
	#region Constants

	private const string PROMPT = "res://UI/Settings/Pages/Controls/KeyPrompt/KeyPrompt.tscn";

	private static readonly (string Action, string Node, string Title)[] ROWS =
	{
		("forward", "ForwardKey", "Move Forward"),
		("left", "LeftKey", "Move Left"),
		("backward", "BackwardKey", "Move Back"),
		("right", "RightKey", "Move Right"),
		("rotateLeft", "RotateLeftKey", "Rotate Camera Left"),
		("rotateRight", "RotateRightKey", "Rotate Camera Right")
	};

	#endregion


	#region Nodes

	private readonly Dictionary<string, Button> ButtonByAction = new();

	private KeyPrompt Prompt;

	#endregion


	#region State

	private string Pending;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		foreach ((string action, string node, string title) in ROWS)
		{
			string captured = action;

			Button button = GetNode<Button>($"%{node}");

			button.Pressed += () => OnRebindPressed(captured);

			ButtonByAction[action] = button;
		}

		Prompt = GD.Load<PackedScene>(PROMPT).Instantiate<KeyPrompt>();
		Prompt.Captured += OnCaptured;

		AddChild(Prompt);

		Refresh();
	}

	#endregion


	#region Content

	public override void Refresh()
	{
		if (Config == null)
		{
			return;
		}

		foreach (KeyValuePair<string, Button> pair in ButtonByAction)
		{
			Key key = (Key)Config.Get(SettingsManager.ControlKey(pair.Key)).AsInt32();

			pair.Value.Text = OS.GetKeycodeString(key);
		}
	}


	private static string TitleOf(string action)
	{
		foreach ((string current, string _, string title) in ROWS)
		{
			if (current == action)
			{
				return title;
			}
		}

		return action;
	}

	#endregion


	#region Actions

	private void OnRebindPressed(string action)
	{
		Pending = action;

		Prompt.Open(TitleOf(action));
	}


	private void OnCaptured(Key key)
	{
		if (Pending == null || Config == null)
		{
			return;
		}

		if (key == Key.Escape)
		{
			Close();
			return;
		}

		string taken = Config.FindBinding(key, Pending);

		if (taken != null)
		{
			Prompt.Reject($"{OS.GetKeycodeString(key)} is already used by {TitleOf(taken)}");
			return;
		}

		Config.Set(SettingsManager.ControlKey(Pending), (int)key);

		Close();
	}


	private void Close()
	{
		Pending = null;

		Prompt.Visible = false;

		Refresh();
	}

	#endregion
}
