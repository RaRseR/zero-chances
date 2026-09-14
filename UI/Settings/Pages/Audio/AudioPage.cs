using Godot;

public partial class AudioPage : SettingsSection
{
	#region Nodes

	private readonly System.Collections.Generic.Dictionary<string, HSlider> Sliders = new();
	private readonly System.Collections.Generic.Dictionary<string, Label> Hints = new();

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		Bind(SettingsManager.AUDIO_MASTER, "MasterSlider", "MasterHint");
		Bind(SettingsManager.AUDIO_MUSIC, "MusicSlider", "MusicHint");
		Bind(SettingsManager.AUDIO_SFX, "SfxSlider", "SfxHint");

		Refresh();
	}

	#endregion


	#region Content

	private void Bind(string key, string slider, string hint)
	{
		HSlider control = GetNode<HSlider>($"%{slider}");

		control.ValueChanged += value => OnChanged(key, (float)value);

		Sliders[key] = control;
		Hints[key] = GetNode<Label>($"%{hint}");
	}


	public override void Refresh()
	{
		if (Config == null)
		{
			return;
		}

		foreach (System.Collections.Generic.KeyValuePair<string, HSlider> pair in Sliders)
		{
			float level = Config.Get(pair.Key).AsSingle();

			pair.Value.SetValueNoSignal(level);

			Hints[pair.Key].Text = Percent(level);
		}
	}


	private static string Percent(float level)
	{
		return $"{Mathf.RoundToInt(level * 100f)}%";
	}

	#endregion


	#region Actions

	private void OnChanged(string key, float level)
	{
		Config?.Set(key, level);

		Hints[key].Text = Percent(level);
	}

	#endregion
}
