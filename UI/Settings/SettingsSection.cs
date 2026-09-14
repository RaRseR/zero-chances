using Godot;

public partial class SettingsSection : VBoxContainer
{
	#region Access

	protected static SettingsManager Config => SettingsManager.Instance;

	#endregion


	#region Content

	public virtual void Refresh()
	{
	}

	#endregion
}
