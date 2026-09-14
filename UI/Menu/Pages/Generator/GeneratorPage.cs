using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

public partial class GeneratorPage : Control
{
	#region Constants

	private const int PREVIEW_SIZE = 640;

	private const int PANEL_WIDTH = 320;

	#endregion


	#region Exports

	[Export] public float VeilAlpha { get; set; } = 0.96f;

	#endregion


	#region Nodes

	private OptionButton SizeOption;
	private OptionButton ShapeOption;
	private OptionButton LayerOption;
	private LineEdit SeedEdit;
	private Button GenerateButton;
	private TextureRect PreviewRect;
	private Label ReportLabel;

	#endregion


	#region State

	private readonly List<Type> SizeTypes = new();
	private readonly List<Type> ShapeTypes = new();
	private SessionConfiguration Configuration;
	private WorldGeneration Result;

	private bool Running;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		CollectTypes();
		BuildInterface();

		SeedEdit.Text = ((int)(GD.Randi() >> 1)).ToString();
	}

	#endregion


	#region Interface

	private void BuildInterface()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);

		MarginContainer margin = new MarginContainer();

		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 48);
		margin.AddThemeConstantOverride("margin_top", 40);
		margin.AddThemeConstantOverride("margin_right", 48);
		margin.AddThemeConstantOverride("margin_bottom", 40);

		AddChild(margin);

		HBoxContainer row = new HBoxContainer();

		row.AddThemeConstantOverride("separation", 24);

		margin.AddChild(row);

		row.AddChild(BuildPanel());
		row.AddChild(BuildPreview());
	}


	private Control BuildPanel()
	{
		VBoxContainer panel = new VBoxContainer();

		panel.CustomMinimumSize = new Vector2(PANEL_WIDTH, 0f);
		panel.SizeFlagsHorizontal = SizeFlags.ShrinkBegin;
		panel.AddThemeConstantOverride("separation", 10);

		panel.AddChild(Caption("WORLD GENERATOR"));

		panel.AddChild(Caption("Size"));
		SizeOption = new OptionButton();
		FillOptions(SizeOption, SizeTypes);
		panel.AddChild(SizeOption);

		panel.AddChild(Caption("Shape"));
		ShapeOption = new OptionButton();
		FillOptions(ShapeOption, ShapeTypes);
		panel.AddChild(ShapeOption);

		panel.AddChild(Caption("Seed"));

		HBoxContainer seedRow = new HBoxContainer();

		seedRow.AddThemeConstantOverride("separation", 8);

		SeedEdit = new LineEdit();
		SeedEdit.SizeFlagsHorizontal = SizeFlags.ExpandFill;

		Button rollButton = new Button();
		rollButton.Text = "Roll";
		rollButton.Pressed += OnRollPressed;

		seedRow.AddChild(SeedEdit);
		seedRow.AddChild(rollButton);

		panel.AddChild(seedRow);

		GenerateButton = new Button();
		GenerateButton.Text = "Generate";
		GenerateButton.CustomMinimumSize = new Vector2(0f, 48f);
		GenerateButton.Pressed += OnGeneratePressed;

		panel.AddChild(GenerateButton);

		panel.AddChild(Caption("Layer"));

		LayerOption = new OptionButton();
		LayerOption.ItemSelected += OnLayerSelected;

		foreach (PreviewMode mode in LayerPreview.MODES)
		{
			LayerOption.AddItem(mode.ToString());
		}

		LayerOption.Select(0);

		panel.AddChild(LayerOption);

		Control filler = new Control();
		filler.SizeFlagsVertical = SizeFlags.ExpandFill;

		panel.AddChild(filler);

		Button backButton = new Button();
		backButton.Text = "Back";
		backButton.Pressed += OnBackPressed;

		panel.AddChild(backButton);

		return panel;
	}


	private Control BuildPreview()
	{
		VBoxContainer column = new VBoxContainer();

		column.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		column.AddThemeConstantOverride("separation", 12);

		PreviewRect = new TextureRect();

		PreviewRect.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		PreviewRect.SizeFlagsVertical = SizeFlags.ExpandFill;
		PreviewRect.ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize;
		PreviewRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
		PreviewRect.TextureFilter = CanvasItem.TextureFilterEnum.Nearest;

		column.AddChild(PreviewRect);

		ScrollContainer report = new ScrollContainer();

		report.CustomMinimumSize = new Vector2(0f, 168f);
		report.HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled;

		ReportLabel = new Label();

		ReportLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		ReportLabel.VerticalAlignment = VerticalAlignment.Top;
		ReportLabel.Text = "No world generated yet.";

		report.AddChild(ReportLabel);

		column.AddChild(report);

		return column;
	}


	private static Label Caption(string text)
	{
		Label label = new Label();

		label.Text = text;

		return label;
	}


	private void FillOptions(OptionButton option, List<Type> types)
	{
		foreach (Type type in types)
		{
			option.AddItem(FeatureRules.Name(type));
		}

		if (types.Count > 0)
		{
			option.Select(0);
		}
	}

	#endregion


	#region Types

	private void CollectTypes()
	{
		Assembly assembly = Assembly.GetExecutingAssembly();

		SizeTypes.AddRange(assembly
			.GetTypes()
			.Where(type => !type.IsAbstract && typeof(WorldSizeFeature).IsAssignableFrom(type))
			.OrderBy(type => ((WorldSizeFeature)Activator.CreateInstance(type)).ChunkCountPerWorldSide));

		ShapeTypes.AddRange(assembly
			.GetTypes()
			.Where(type => !type.IsAbstract && typeof(WorldShapeFeature).IsAssignableFrom(type))
			.OrderBy(type => type.Name));
	}

	#endregion


	#region Actions

	private void OnRollPressed()
	{
		SeedEdit.Text = ((int)(GD.Randi() >> 1)).ToString();
	}


	private void OnBackPressed()
	{
		MenuRoot.FindIn(this)?.Back();
	}


	private void OnGeneratePressed()
	{
		if (Running || SizeTypes.Count == 0 || ShapeTypes.Count == 0)
		{
			return;
		}

		WorldSizeFeature size = (WorldSizeFeature)Activator.CreateInstance(SizeTypes[SizeOption.Selected]);
		WorldShapeFeature shape = (WorldShapeFeature)Activator.CreateInstance(ShapeTypes[ShapeOption.Selected]);

		Configuration = new SessionConfiguration(ResolveSeed(), size, shape, null);

		Running = true;

		GenerateButton.Disabled = true;
		ReportLabel.Text = $"Generating {Configuration.PointCountPerWorldSide} x {Configuration.PointCountPerWorldSide} ...";

		SessionConfiguration configuration = Configuration;

		Task.Run(() =>
		{
			WorldGeneration generation = Generator.Generate(configuration);

			Result = generation;

			Callable.From(OnGenerated).CallDeferred();
		});
	}


	private void OnGenerated()
	{
		Running = false;

		GenerateButton.Disabled = false;

		RefreshPreview();
		RefreshReport();
	}


	private void OnLayerSelected(long index)
	{
		RefreshPreview();
	}


	private int ResolveSeed()
	{
		string text = SeedEdit.Text.Trim();

		if (int.TryParse(text, out int number))
		{
			return number;
		}

		return text.Length > 0 ? text.GetHashCode() : (int)(GD.Randi() >> 1);
	}

	#endregion


	#region Output

	private void RefreshPreview()
	{
		if (Result == null)
		{
			return;
		}

		int index = Mathf.Clamp(LayerOption.Selected, 0, LayerPreview.MODES.Length - 1);

		PreviewRect.Texture = LayerPreview.Build(Result.Layers, Configuration, LayerPreview.MODES[index], PREVIEW_SIZE);
	}


	private void RefreshReport()
	{
		if (Result == null)
		{
			return;
		}

		StringBuilder builder = new StringBuilder();

		builder.AppendLine(
			$"seed {Result.Seed}   {Configuration.PointCountPerWorldSide} x {Configuration.PointCountPerWorldSide} points" +
			$"   {Result.Layers.ByteSize / (1024 * 1024)} MB");

		builder.AppendLine();

		for (int index = 0; index < Result.Plan.Length; index++)
		{
			Step step = Result.Plan[index];

			builder.AppendLine($"{index,3}  {step.GetType().Name,-22} {Result.Milliseconds[index],8:F1} ms");
		}

		builder.AppendLine();
		builder.AppendLine($"{"total",-27} {Result.TotalMilliseconds,8:F1} ms");

		ReportLabel.Text = builder.ToString();
	}

	#endregion
}
