using Godot;
using System;

public partial class Camera : Camera3D
{
	#region Export - Movement

	[Export] public float CameraSpeed = 2_500f;
	[Export] public float SprintMultiplier = 3f;

	[Export] public float EdgeScrollMargin = 10f;

	#endregion


	#region Export - Zoom

	[Export] public float ZoomStep = 250f;
	[Export] public float ZoomMin = 4_000f;
	[Export] public float ZoomMax = 15_000f;

	#endregion


	#region Export - Rotation

	[Export] public float YawSensitivity = 0.50f;
	[Export] public float PitchSensitivity = 0.18f;
	[Export] public float MaxStepDeg = 3f;

	[Export] public float PitchMinDeg = 10f;
	[Export] public float PitchMaxDeg = 80f;

	#endregion


	#region State
	
	[Export] public Vector3 StartOrbitCenter = new Vector3(25_000f, 0f, 25_000f);
	[Export] public float StartOrbitDistance = 10_000f;

	public Vector3 OrbitCenter;
	public float OrbitDistance;
	
	[Export] public float StartYawDeg = 0f;
	[Export] public float StartPitchDeg = 45f;

	private float Yaw;
	private float Pitch;

	private bool IsRotating;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		OrbitCenter = StartOrbitCenter;
		OrbitDistance = Mathf.Clamp(StartOrbitDistance, ZoomMin, ZoomMax);
		
		Yaw = Mathf.DegToRad(StartYawDeg);
		Pitch = Mathf.Clamp(Pitch, Mathf.DegToRad(PitchMinDeg), Mathf.DegToRad(PitchMaxDeg));
		
		UpdateCameraPosition();
	}


	public override void _Process(double delta)
	{
		Vector3 input = GetMovementInput();

		if (input.LengthSquared() <= 0f) return;

		Vector3 forward = new Vector3(-Mathf.Sin(Yaw), 0f, -Mathf.Cos(Yaw));
		Vector3 right = new Vector3(Mathf.Cos(Yaw), 0f, -Mathf.Sin(Yaw));

		Vector3 movement = (right * input.X + forward * -input.Z).Normalized();

		float speed = CameraSpeed * (Input.IsActionPressed("shift") ? SprintMultiplier : 1f);

		OrbitCenter += movement * speed * (float)delta;

		UpdateCameraPosition();
	}


	public override void _UnhandledInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseButton button)
		{
			HandleMouseButton(button);
		}
		else if (inputEvent is InputEventMouseMotion motion && IsRotating)
		{
			HandleMouseMotion(motion);
		}
	}

	#endregion


	#region Input Handling

	private Vector3 GetMovementInput()
	{
		var input = Vector3.Zero;

		if (Input.IsActionPressed("right")) input.X += 1;
		if (Input.IsActionPressed("left")) input.X -= 1;
		if (Input.IsActionPressed("forward")) input.Z -= 1;
		if (Input.IsActionPressed("backward")) input.Z += 1;

		Vector2 mouse = GetViewport().GetMousePosition();
		Vector2 size = GetViewport().GetVisibleRect().Size;

		if (mouse.X < EdgeScrollMargin) input.X -= 1;
		else if (mouse.X > size.X - EdgeScrollMargin) input.X += 1;

		if (mouse.Y < EdgeScrollMargin) input.Z -= 1;
		else if (mouse.Y > size.Y - EdgeScrollMargin) input.Z += 1;

		return input;
	}


	private void HandleMouseButton(InputEventMouseButton button)
	{
		if (button.ButtonIndex == MouseButton.WheelUp && button.Pressed)
		{
			OrbitDistance = Mathf.Max(ZoomMin, OrbitDistance - ZoomStep);
			UpdateCameraPosition();
		}

		if (button.ButtonIndex == MouseButton.WheelDown && button.Pressed)
		{
			OrbitDistance = Mathf.Min(ZoomMax, OrbitDistance + ZoomStep);
			UpdateCameraPosition();
		}

		if (button.ButtonIndex == MouseButton.Middle)
		{
			IsRotating = button.Pressed;
		}
	}


	private void HandleMouseMotion(InputEventMouseMotion motion)
	{
		Vector2 viewportSize = GetViewport().GetVisibleRect().Size;
		float minSide = Mathf.Min(viewportSize.X, viewportSize.Y);

		float dx = motion.Relative.X / minSide * YawSensitivity * Mathf.Tau;
		float dy = motion.Relative.Y / minSide * PitchSensitivity * Mathf.Tau;

		float maxStep = Mathf.DegToRad(MaxStepDeg);
		dx = Mathf.Clamp(dx, -maxStep, maxStep);
		dy = Mathf.Clamp(dy, -maxStep, maxStep);

		Yaw -= dx;
		Pitch = Mathf.Clamp(Pitch + dy, Mathf.DegToRad(PitchMinDeg), Mathf.DegToRad(PitchMaxDeg));

		UpdateCameraPosition();
	}

	#endregion

	private void UpdateCameraPosition()
	{
		Vector3 orbitOffset = new Vector3(
			Mathf.Sin(Yaw) * Mathf.Cos(Pitch),
			Mathf.Sin(Pitch),
			Mathf.Cos(Yaw) * Mathf.Cos(Pitch)
		) * OrbitDistance;
		
		GlobalPosition = OrbitCenter + orbitOffset;

		LookAt(OrbitCenter, Vector3.Up);
	}
}
