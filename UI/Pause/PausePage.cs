using Godot;
using System;

public partial class PausePage : Control
{
	#region Constants

	private const string SETTINGS_PAGE = "res://UI/Settings/SettingsPage.tscn";

	#endregion


	#region State

	public event Action ExitRequested;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		ProcessMode = ProcessModeEnum.Always;

		GetNode<Button>("Center/Box/ResumeButton").Pressed += OnResumePressed;
		GetNode<Button>("Center/Box/SettingsButton").Pressed += OnSettingsPressed;
		GetNode<Button>("Center/Box/ExitButton").Pressed += OnExitPressed;

		GetTree().Paused = true;
	}

	#endregion


	#region Actions

	private void OnResumePressed()
	{
		GetTree().Paused = false;
		QueueFree();
	}


	private void OnSettingsPressed()
	{
		PackedScene scene = GD.Load<PackedScene>(SETTINGS_PAGE);

		if (scene == null)
		{
			GD.PushError($"PausePage: не найдена сцена {SETTINGS_PAGE}");
			return;
		}

		AddChild(scene.Instantiate<Control>());
	}


	private void OnExitPressed()
	{
		GetTree().Paused = false;

		ExitRequested?.Invoke();

		QueueFree();
	}

	#endregion
}
