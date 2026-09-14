using Godot;

public partial class Background : Node3D
{
	#region Properties

	[Export] public float CameraDistance { get; set; } = 40_000f;

	#endregion
}
