using Godot;

public partial class EmptySlot : DashedBorder
{
	#region Nodes

	private Label NumberLabel;

	#endregion


	#region State

	private int Number;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		NumberLabel = GetNode<Label>("%NumberLabel");

		Apply();
	}

	#endregion


	#region Content

	public void Setup(int number)
	{
		Number = number;

		Apply();
	}


	private void Apply()
	{
		if (NumberLabel == null)
		{
			return;
		}

		NumberLabel.Text = $"SLOT {Number}";
	}

	#endregion
}
