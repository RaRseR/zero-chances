using Godot;
using System;
using System.Collections.Generic;

public partial class MenuRoot : Control
{
	#region Constants

	private const string START_PAGE = "res://UI/Menu/Pages/Start/StartPage.tscn";

	private const float DEFAULT_VEIL = 0.9f;

	#endregion


	#region Nodes

	private Control PageHost;
	private ColorRect Veil;

	#endregion


	#region State

	private readonly List<Control> PageStack = new();

	public bool CanGoBack => PageStack.Count > 1;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		PageHost = GetNode<Control>("%PageHost");
		Veil = GetNode<ColorRect>("%Veil");

		Open(START_PAGE);
	}


	public override void _Input(InputEvent input)
	{
		if (input is not InputEventMouseButton button || !button.Pressed)
		{
			return;
		}

		Control focused = GetViewport().GuiGetFocusOwner();

		if (focused is LineEdit edit && !edit.GetGlobalRect().HasPoint(button.GlobalPosition))
		{
			edit.ReleaseFocus();
		}
	}

	#endregion


	#region Backdrop

	public void SetVeil(float alpha)
	{
		Color color = Veil.Color;

		color.A = Mathf.Clamp(alpha, 0f, 1f);

		Veil.Color = color;
	}


	private void ApplyVeil(Control page)
	{
		Variant alpha = page.Get("VeilAlpha");

		SetVeil(alpha.VariantType == Variant.Type.Float ? (float)alpha : DEFAULT_VEIL);
	}

	#endregion


	#region Navigation

	public Control Open(string scenePath, Action<Control> configure = null)
	{
		PackedScene scene = GD.Load<PackedScene>(scenePath);

		if (scene == null)
		{
			GD.PushError($"MenuRoot: scene not found - {scenePath}");
			return null;
		}

		if (PageStack.Count > 0)
		{
			PageStack[^1].Visible = false;
		}

		Control page = scene.Instantiate<Control>();

		configure?.Invoke(page);

		PageStack.Add(page);
		PageHost.AddChild(page);

		ApplyVeil(page);

		return page;
	}


	public void Back()
	{
		if (!CanGoBack)
		{
			return;
		}

		Control closing = PageStack[^1];
		PageStack.RemoveAt(PageStack.Count - 1);

		PageHost.RemoveChild(closing);
		closing.QueueFree();

		PageStack[^1].Visible = true;

		ApplyVeil(PageStack[^1]);
	}


	public static MenuRoot FindIn(Node node)
	{
		for (Node current = node; current != null; current = current.GetParent())
		{
			if (current is MenuRoot menu)
			{
				return menu;
			}
		}

		return null;
	}

	#endregion
}
