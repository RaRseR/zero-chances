using Godot;
using System;

public sealed class SettingDefinition
{
	#region State

	public Variant Default;

	public Action<Variant> Apply;

	#endregion
}
