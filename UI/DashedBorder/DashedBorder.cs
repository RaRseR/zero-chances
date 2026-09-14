using Godot;

[Tool]
[GlobalClass]
public partial class DashedBorder : PanelContainer
{
	#region State

	private Color Line = new Color(0.13725491f, 0.12941177f, 0.12156863f, 0.3f);

	private float Thickness = 3f;
	private float Dash = 12f;
	private float Gap = 9f;

	#endregion


	#region Exports

	[Export]
	public Color LineColor
	{
		get => Line;
		set
		{
			Line = value;
			QueueRedraw();
		}
	}


	[Export]
	public float LineWidth
	{
		get => Thickness;
		set
		{
			Thickness = value;
			QueueRedraw();
		}
	}


	[Export]
	public float DashLength
	{
		get => Dash;
		set
		{
			Dash = value;
			QueueRedraw();
		}
	}


	[Export]
	public float GapLength
	{
		get => Gap;
		set
		{
			Gap = value;
			QueueRedraw();
		}
	}

	#endregion


	#region Godot Callbacks

	public override void _Draw()
	{
		float inset = Thickness * 0.5f;

		float left = inset;
		float right = Size.X - inset;
		float top = inset;
		float bottom = Size.Y - inset;

		DrawEdge(new Vector2(0f, top), new Vector2(Size.X, top));
		DrawEdge(new Vector2(0f, bottom), new Vector2(Size.X, bottom));

		DrawEdge(new Vector2(left, Thickness), new Vector2(left, Size.Y - Thickness));
		DrawEdge(new Vector2(right, Thickness), new Vector2(right, Size.Y - Thickness));
	}

	#endregion


	#region Drawing

	private void DrawEdge(Vector2 from, Vector2 to)
	{
		float length = from.DistanceTo(to);

		if (length <= 0f || Dash <= 0f)
		{
			return;
		}

		int count = Mathf.Max(Mathf.RoundToInt((length + Gap) / (Dash + Gap)), 2);

		float scale = length / (count * Dash + (count - 1) * Gap);

		float dash = Dash * scale;
		float gap = Gap * scale;

		Vector2 step = (to - from) / length;

		for (int index = 0; index < count; index++)
		{
			float offset = index * (dash + gap);

			DrawLine(from + step * offset, from + step * (offset + dash), Line, Thickness);
		}
	}

	#endregion
}
