using Godot;
using System;
using System.Collections.Generic;

public partial class Camera : Camera3D
{
	#region Movement
	
	private float MoveSpeedTexels = 220f;
	private float SprintMultiplier = 3f;
	private bool EdgeScrollEnabled = false;
	private float EdgeScrollMargin = 10f; 
	
	#endregion
	
	
	#region Rotation
	
	private float Yaw = 0f;
	private float TargetYaw = 0f;
	private float YawSensitivity = 0.50f;
	private float SnapSpeedDeg = 360f;
	
	private float Pitch = 45f;
	private float TargetPitch = 45f;
	private float PitchSensitivity = 0.18f;
	private float PitchSnapSpeedDeg = 360f;
	private float MaxStepDeg = 3f;
	private float PitchMinDeg = 10f;
	private float PitchMaxDeg = 89f;
	
	#endregion
	
	
	#region Orbit
	
	private Vector3 OrbitTarget = Vector3.Zero;
	private float OrbitDistance = 40_000f;
	
	#endregion
	
	
	#region Zoom
	
	public event Action<float> ZoomChanged;
	
	public float TargetPxSize => ZOOM_LEVELS[ZoomIndex];
	
	private static readonly float[] ZOOM_LEVELS = { 12.5f, 18f, 25f, 35f, 50f, 70f, 100f, 140f };
	
	private const int DEFAULT_ZOOM_INDEX = 5;
	
	private int ZoomIndex = DEFAULT_ZOOM_INDEX;
	private float PxSize = ZOOM_LEVELS[DEFAULT_ZOOM_INDEX];
	private float ZoomSharpness = 18f;
 
	#endregion
	
	
	#region PixelSnap
	
	public event Action<Vector2> PixelSnapped;
 
	public Vector2 TexelError { get; private set; } = Vector2.Zero;
 
	private Vector2I ViewportSize = Vector2I.One;
	private Transform3D Grid;
	private Basis LastBasis;
 
	private Callable MoveTrackedCallable;
 
	private readonly List<Node3D> Tracked = new();
	private readonly List<Vector3> Origins = new();
 
	#endregion
	
	
	#region Godot Callbacks
	
	public override void _Ready()
	{
		MoveTrackedCallable = Callable.From(MoveTracked);
		
		UpdateOrbitTransform();
		
		RenderingServer.Singleton.Connect(RenderingServer.SignalName.FramePostDraw, Callable.From(RestoreTracked));
	}
	
	
	public override void _Process(double delta)
	{
		UpdateOrbitTarget(delta);
		UpdateYawSnap(delta);
		UpdatePitchSnap(delta);
		UpdateZoomSnap(delta);
		UpdateOrbitTransform();
		UpdatePixelSnap();
	}
	
	
	public override void _UnhandledInput(InputEvent inputEvent)
	{
		HandleOrbitInput(inputEvent);
		HandleZoomInput(inputEvent);
		HandleSnapRotationInput(inputEvent);
		HandleResetInput(inputEvent);
	}
 
	#endregion
	
	
	#region Setup
	
	public void SetupCamera(Vector3 center, float worldSide)
	{
		OrbitTarget = center;
		OrbitDistance = worldSide;
 
		Near = 0f;
		Far = OrbitDistance + worldSide * Mathf.Sqrt2;
 
		UpdateOrbitTransform();
	}
	
	
	public void SetViewportSize(Vector2I size)
	{
		ViewportSize = size;
 
		ApplyZoom();
	}
	
	#endregion

	
	#region Movement
	
	private void UpdateOrbitTarget(double delta)
	{
		Vector3 input = GetMovementInput();
		if (input.LengthSquared() <= 0f) return;
 
		Vector3 forward = -GlobalTransform.Basis.Z;
		forward.Y = 0f;
		forward = forward.Normalized();
 
		Vector3 right = GlobalTransform.Basis.X;
		right.Y = 0f;
		right = right.Normalized();
 
		Vector3 movement = (right * input.X + forward * -input.Z).Normalized();
		float speed = MoveSpeedTexels * PxSize * (Input.IsActionPressed("shift") ? SprintMultiplier : 1f);
 
		OrbitTarget += movement * speed * (float)delta;
	}
 
 
	private Vector3 GetMovementInput()
	{
		Vector3 input = Vector3.Zero;
 
		if (Input.IsActionPressed("right"))
		{
			input.X += 1;
		}
		if (Input.IsActionPressed("left"))
		{
			input.X -= 1;
		}
		if (Input.IsActionPressed("forward"))
		{
			input.Z -= 1;
		}
		if (Input.IsActionPressed("backward"))
		{
			input.Z += 1;
		}
 
		if (EdgeScrollEnabled)
		{
			Vector2 mouse = GetViewport().GetMousePosition();
			Vector2 size = GetViewport().GetVisibleRect().Size;
 
			if (mouse.X < EdgeScrollMargin)
			{
				input.X -= 1;
			}
			else if (mouse.X > size.X - EdgeScrollMargin)
			{
				input.X += 1;
			}
			if (mouse.Y < EdgeScrollMargin)
			{
				input.Z -= 1;
			}
			else if (mouse.Y > size.Y - EdgeScrollMargin)
			{
				input.Z += 1;
			}
		}
 
		return input;
	}
 
	#endregion
	
	
	#region Orbit
 
	private void UpdateOrbitTransform()
	{
		float yawRad = Mathf.DegToRad(Yaw);
		float pitchRad = Mathf.DegToRad(Pitch);
 
		Vector3 offset = new Vector3(
			Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
			Mathf.Sin(pitchRad),
			Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
		) * OrbitDistance;
 
		GlobalPosition = OrbitTarget + offset;
 
		LookAt(OrbitTarget, Vector3.Up);
	}
 
 
	private void UpdateYawSnap(double delta)
	{
		Yaw = Mathf.MoveToward(Yaw, TargetYaw, SnapSpeedDeg * (float)delta);
	}
 
 
	private void UpdatePitchSnap(double delta)
	{
		Pitch = Mathf.MoveToward(Pitch, TargetPitch, PitchSnapSpeedDeg * (float)delta);
	}
 
	#endregion
 
 
	#region Zoom
 
	
	private void UpdateZoomSnap(double delta)
	{
		float target = ZOOM_LEVELS[ZoomIndex];
		
		if (Mathf.IsEqualApprox(PxSize, target))
		{
			return;
		}
		
		float weight = 1f - Mathf.Exp(-ZoomSharpness * (float)delta);
		
		PxSize = Mathf.Lerp(PxSize, target, weight);
		
		if (Mathf.Abs(target - PxSize) < 0.01f)
		{
			PxSize = target;
		}
		
		ApplyZoom();
	}
	
	
	private void SetZoomIndex(int index)
	{
		int clamped = Mathf.Clamp(index, 0, ZOOM_LEVELS.Length - 1);
		
		if (clamped == ZoomIndex) return;
		
		ZoomIndex = clamped;
		
		ZoomChanged?.Invoke(TargetPxSize);
	}
	
	
	private void ApplyZoom()
	{
		Size = PxSize * ViewportSize.Y;
	}
 
	#endregion
	
	
	#region PixelSnap
 
	private void UpdatePixelSnap()
	{
		Basis basis = GlobalTransform.Basis;
		if (!basis.IsEqualApprox(LastBasis))
		{
			LastBasis = basis;
			Grid = GlobalTransform;
		}
 
		Vector3 localPos = Grid.AffineInverse() * GlobalPosition;
		Vector3 aligned = localPos.Snapped(Vector3.One * PxSize);
		Vector3 drift = aligned - localPos;
 
		HOffset = drift.X;
		VOffset = drift.Y;
 
		TexelError = new Vector2(drift.X, -drift.Y) / PxSize;
 
		PixelSnapped?.Invoke(TexelError);
 
		if (GetTree().HasGroup("snap"))
		{
			MoveTrackedCallable.CallDeferred();
		}
	}
 
 
	private void MoveTracked()
	{
		Tracked.Clear();
		Origins.Clear();
 
		foreach (Node node in GetTree().GetNodesInGroup("snap"))
		{
			if (node is not Node3D obj) continue;
 
			Tracked.Add(obj);
			Origins.Add(obj.GlobalPosition);
 
			Vector3 inGrid = Grid.AffineInverse() * obj.GlobalPosition;
			Vector3 nudged = inGrid.Snapped(new Vector3(PxSize, PxSize, 0f));
 
			obj.GlobalPosition = Grid * nudged;
		}
	}
 
 
	private void RestoreTracked()
	{
		for (int i = 0; i < Tracked.Count; i++)
		{
			Tracked[i].GlobalPosition = Origins[i];
		}
 
		Tracked.Clear();
	}
 
	#endregion
	
	
	#region Input
 
	private void HandleOrbitInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseMotion mouseMotion && Input.IsMouseButtonPressed(MouseButton.Middle))
		{
			Yaw -= mouseMotion.Relative.X * YawSensitivity;
			TargetYaw = Yaw;
 
			Pitch = Mathf.Clamp(Pitch + mouseMotion.Relative.Y * PitchSensitivity, PitchMinDeg, PitchMaxDeg);
			TargetPitch = Pitch;
		}
	}
 
 
	private void HandleZoomInput(InputEvent inputEvent)
	{
		if (inputEvent is not InputEventMouseButton mouseButton || !mouseButton.Pressed) return;
		
		if (mouseButton.ButtonIndex == MouseButton.WheelUp)
		{
			SetZoomIndex(ZoomIndex - 1);
		}
		else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
		{
			SetZoomIndex(ZoomIndex + 1);
		}
	}

 
	private void HandleSnapRotationInput(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed("rotateLeft"))
		{
			TargetYaw = Mathf.Round(TargetYaw / 90f) * 90f - 90f;
		}
		else if (inputEvent.IsActionPressed("rotateRight"))
		{
			TargetYaw = Mathf.Round(TargetYaw / 90f) * 90f + 90f;
		}
	}
 
 
	private void HandleResetInput(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed("reset"))
		{
			TargetPitch = 45f;
			SetZoomIndex(DEFAULT_ZOOM_INDEX);
		}
	}
 
	#endregion
}
