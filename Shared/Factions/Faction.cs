using Godot;

public sealed class Faction
{
	#region Constants

	private const string SHEET_PATH = "res://Shared/Factions/Icons.png";

	private const int ICON_SIZE = 32;
	private const int ICON_COLUMNS = 8;

	#endregion


	#region State

	public string Title = string.Empty;
	public int Index = -1;

	public Color Base;
	public Color Text;

	private Texture2D LoadedIcon;
	private bool IconResolved;

	private static Texture2D LoadedSheet;
	private static bool SheetResolved;

	#endregion


	#region Access

	public Texture2D Icon
	{
		get
		{
			if (!IconResolved)
			{
				IconResolved = true;
				LoadedIcon = BuildIcon(Index);
			}

			return LoadedIcon;
		}
	}


	private static Texture2D Sheet
	{
		get
		{
			if (!SheetResolved)
			{
				SheetResolved = true;

				if (ResourceLoader.Exists(SHEET_PATH))
				{
					LoadedSheet = GD.Load<Texture2D>(SHEET_PATH);
				}
			}

			return LoadedSheet;
		}
	}


	private static AtlasTexture BuildIcon(int index)
	{
		Texture2D sheet = Sheet;

		if (sheet == null || index < 0)
		{
			return null;
		}

		AtlasTexture icon = new AtlasTexture();

		icon.Atlas = sheet;
		icon.FilterClip = true;

		icon.Region = new Rect2(
			index % ICON_COLUMNS * ICON_SIZE,
			index / ICON_COLUMNS * ICON_SIZE,
			ICON_SIZE,
			ICON_SIZE
		);

		return icon;
	}

	#endregion


	#region Construction

	public Faction(string title, string baseHex, string textHex)
	{
		Title = title;
		Base = new Color(baseHex);
		Text = new Color(textHex);
	}

	#endregion
}
