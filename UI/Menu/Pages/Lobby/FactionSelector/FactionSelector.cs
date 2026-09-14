using Godot;
using System;
using System.Collections.Generic;

public partial class FactionSelector : Control
{
	#region Constants

	private const string CARD = "res://UI/Menu/Pages/Lobby/FactionSelector/FactionCard.tscn";

	#endregion


	#region Events

	public event Action<int> Chosen;

	#endregion


	#region Nodes

	private GridContainer Grid;
	private PanelContainer Preview;
	private Label PreviewName;
	private ColorRect Dim;

	#endregion


	#region State

	private readonly List<FactionCard> Cards = new();

	private int Current = -1;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		Grid = GetNode<GridContainer>("%Grid");
		Preview = GetNode<PanelContainer>("%Preview");
		PreviewName = GetNode<Label>("%PreviewName");
		Dim = GetNode<ColorRect>("%Dim");

		Dim.GuiInput += OnDimInput;

		Build();
	}

	#endregion


	#region Content

	private void Build()
	{
		PackedScene scene = GD.Load<PackedScene>(CARD);

		for (int index = 0; index < Factions.COUNT; index++)
		{
			int captured = index;

			FactionCard card = scene.Instantiate<FactionCard>();

			Grid.AddChild(card);

			card.Pressed += () => OnCardPressed(captured);
			card.MouseEntered += () => ShowPreview(captured);
			card.MouseExited += () => ShowPreview(Current);

			Cards.Add(card);
		}
	}


	public void Open(IReadOnlyList<Player> players, Player local)
	{
		Player[] holders = new Player[Factions.COUNT];

		foreach (Player player in players)
		{
			if (player.FactionIndex >= 0 && player.FactionIndex < holders.Length)
			{
				holders[player.FactionIndex] = player;
			}
		}

		Current = local?.FactionIndex ?? -1;

		for (int index = 0; index < Cards.Count; index++)
		{
			Player holder = holders[index];

			bool mine = holder != null && holder == local;
			bool locked = holder != null && !holder.IsBot && !mine;

			Cards[index].Setup(Factions.Get(index), BuildOwner(holder, mine), mine, locked);
		}

		ShowPreview(Current);

		Visible = true;
	}


	private void OnDimInput(InputEvent input)
	{
		if (input is InputEventMouseButton button && button.Pressed)
		{
			Visible = false;
		}
	}


	private static string BuildOwner(Player holder, bool mine)
	{
		if (holder == null || mine)
		{
			return string.Empty;
		}

		return holder.Name.ToUpperInvariant();
	}


	private void ShowPreview(int index)
	{
		Faction faction = Factions.Get(index);

		PreviewName.Text = faction.Title;
		PreviewName.AddThemeColorOverride("font_color", faction.Text);

		if (Preview.GetThemeStylebox("panel").Duplicate() is StyleBoxFlat box)
		{
			box.BgColor = faction.Base;
			box.BorderColor = Factions.Shade(faction.Base);

			Preview.AddThemeStyleboxOverride("panel", box);
		}
	}


	private void OnCardPressed(int index)
	{
		Visible = false;

		Chosen?.Invoke(index);
	}

	#endregion
}
