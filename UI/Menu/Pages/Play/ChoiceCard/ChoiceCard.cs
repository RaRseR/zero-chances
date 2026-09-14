using Godot;

[Tool]
[GlobalClass]
public partial class ChoiceCard : Button
{
	#region Nodes

	private Label CaptionLabel;
	private TextureRect ArtRect;

	#endregion


	#region State

	private string CaptionText = "Card";
	private Texture2D ArtTexture;

	#endregion


	#region Exports

	[Export]
	public string Caption
	{
		get => CaptionText;
		set
		{
			CaptionText = value;
			Apply();
		}
	}

	[Export]
	public Texture2D Art
	{
		get => ArtTexture;
		set
		{
			ArtTexture = value;
			Apply();
		}
	}

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		CaptionLabel = GetNode<Label>("%CaptionLabel");
		ArtRect = GetNode<TextureRect>("%ArtRect");

		Apply();
	}

	#endregion


	#region Content

	private void Apply()
	{
		if (CaptionLabel == null)
		{
			return;
		}

		CaptionLabel.Text = CaptionText;

		ArtRect.Texture = ArtTexture;
	}

	#endregion
}
