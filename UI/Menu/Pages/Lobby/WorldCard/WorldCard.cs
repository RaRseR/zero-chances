using Godot;

public partial class WorldCard : Button
{
	#region Nodes

	private Label TitleLabel;
	private Label CategoryLabel;
	private Label StatLabel;

	#endregion


	#region State

	private string TitleText = "Card";
	private string CategoryText = string.Empty;
	private string StatText = string.Empty;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		TitleLabel = GetNode<Label>("%TitleLabel");
		CategoryLabel = GetNode<Label>("%CategoryLabel");
		StatLabel = GetNode<Label>("%StatLabel");

		Apply();
	}

	#endregion


	#region Content

	public void Setup(string title, string category, string stat)
	{
		TitleText = title;
		CategoryText = category;
		StatText = stat;

		Apply();
	}


	private void Apply()
	{
		if (TitleLabel == null)
		{
			return;
		}

		TitleLabel.Text = TitleText;
		CategoryLabel.Text = CategoryText;
		StatLabel.Text = StatText;
	}

	#endregion
}
