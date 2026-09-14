using Godot;

public partial class FactionCard : Button
{
	#region Constants

	private const int SELECTED_BORDER = 5;

	private static readonly Color DIMMED = new Color(1f, 1f, 1f, 0.4f);

	private static readonly string[] STATES =
	{
		"normal", "hover", "hover_pressed", "pressed", "disabled"
	};

	#endregion


	#region Nodes

	private Label NameLabel;
	private Label OwnerLabel;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		NameLabel = GetNode<Label>("%NameLabel");
		OwnerLabel = GetNode<Label>("%OwnerLabel");
	}

	#endregion


	#region Content

	public void Setup(Faction faction, string owner, bool mine, bool locked)
	{
		NameLabel.Text = faction.Title;

		OwnerLabel.Text = owner;
		OwnerLabel.Visible = owner.Length > 0;

		Disabled = locked;
		Modulate = locked ? DIMMED : Colors.White;

		Paint(faction, mine);

		NameLabel.AddThemeColorOverride("font_color", faction.Text);
		OwnerLabel.AddThemeColorOverride("font_color", faction.Text);
	}


	private void Paint(Faction faction, bool selected)
	{
		Color border = Factions.Shade(faction.Base);

		foreach (string state in STATES)
		{
			if (GetThemeStylebox(state).Duplicate() is not StyleBoxFlat box)
			{
				continue;
			}

			box.BgColor = faction.Base;
			box.BorderColor = border;

			if (selected)
			{
				box.SetBorderWidthAll(SELECTED_BORDER);
			}

			AddThemeStyleboxOverride(state, box);
		}
	}

	#endregion
}
