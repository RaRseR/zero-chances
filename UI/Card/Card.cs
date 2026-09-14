using Godot;

[Tool]
[GlobalClass]
public partial class Card : Button
{
	#region Nodes

	private Label TitleLabel;
	private Label CostLabel;
	private TextureRect ArtRect;
	private Label TypeLabel;
	private Label RarityLabel;
	private Label RulesLabel;
	private HBoxContainer Stats;
	private Label AttackLabel;
	private Label HealthLabel;

	#endregion


	#region State

	private string TitleText = "Card";
	private string TypeText = "UNIT";
	private string RarityText = "COMMON";
	private string RulesText = string.Empty;

	private Texture2D ArtTexture;

	private int CostValue;
	private int AttackValue;
	private int HealthValue;

	private bool StatsVisible = true;
	private bool CostVisible = true;

	#endregion


	#region Exports

	[Export]
	public string Title
	{
		get => TitleText;
		set
		{
			TitleText = value;
			Apply();
		}
	}


	[Export]
	public int Cost
	{
		get => CostValue;
		set
		{
			CostValue = value;
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


	[Export]
	public string Type
	{
		get => TypeText;
		set
		{
			TypeText = value;
			Apply();
		}
	}


	[Export]
	public string Rarity
	{
		get => RarityText;
		set
		{
			RarityText = value;
			Apply();
		}
	}


	[Export(PropertyHint.MultilineText)]
	public string Rules
	{
		get => RulesText;
		set
		{
			RulesText = value;
			Apply();
		}
	}


	[Export]
	public bool ShowCost
	{
		get => CostVisible;
		set
		{
			CostVisible = value;
			Apply();
		}
	}


	[Export]
	public bool ShowStats
	{
		get => StatsVisible;
		set
		{
			StatsVisible = value;
			Apply();
		}
	}


	[Export]
	public int Attack
	{
		get => AttackValue;
		set
		{
			AttackValue = value;
			Apply();
		}
	}


	[Export]
	public int Health
	{
		get => HealthValue;
		set
		{
			HealthValue = value;
			Apply();
		}
	}

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		TitleLabel = GetNode<Label>("%TitleLabel");
		CostLabel = GetNode<Label>("%CostLabel");
		ArtRect = GetNode<TextureRect>("%ArtRect");
		TypeLabel = GetNode<Label>("%TypeLabel");
		RarityLabel = GetNode<Label>("%RarityLabel");
		RulesLabel = GetNode<Label>("%RulesLabel");
		Stats = GetNode<HBoxContainer>("%Stats");
		AttackLabel = GetNode<Label>("%AttackLabel");
		HealthLabel = GetNode<Label>("%HealthLabel");

		Apply();
	}

	#endregion


	#region Content

	private void Apply()
	{
		if (TitleLabel == null)
		{
			return;
		}

		TitleLabel.Text = TitleText;
		CostLabel.Text = CostValue.ToString();

		ArtRect.Texture = ArtTexture;

		CostLabel.Visible = CostVisible;

		TypeLabel.Text = TypeText;

		RarityLabel.Text = RarityText;
		RarityLabel.Visible = RarityText.Length > 0;

		RulesLabel.Text = RulesText;

		Stats.Visible = StatsVisible;

		AttackLabel.Text = AttackValue.ToString();
		HealthLabel.Text = HealthValue.ToString();
	}

	#endregion
}
