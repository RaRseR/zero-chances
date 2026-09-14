using Godot;

public partial class GameplayPage : SettingsSection
{
	#region Constants

	private static readonly (string Locale, string Title)[] LOCALES =
	{
		("en", "English"),
		("ru", "Русский")
	};

	#endregion


	#region Nodes

	private OptionButton LocaleOption;

	#endregion


	#region Godot Callbacks

	public override void _Ready()
	{
		LocaleOption = GetNode<OptionButton>("%LocaleOption");

		LocaleOption.Clear();

		for (int index = 0; index < LOCALES.Length; index++)
		{
			LocaleOption.AddItem(LOCALES[index].Title, index);
		}

		LocaleOption.ItemSelected += OnLocaleSelected;

		Refresh();
	}

	#endregion


	#region Content

	public override void Refresh()
	{
		if (Config == null)
		{
			return;
		}

		string locale = Config.Get(SettingsManager.GAMEPLAY_LOCALE).AsString();

		for (int index = 0; index < LOCALES.Length; index++)
		{
			if (LOCALES[index].Locale == locale)
			{
				LocaleOption.Selected = index;
				return;
			}
		}

		LocaleOption.Selected = 0;
	}

	#endregion


	#region Actions

	private void OnLocaleSelected(long index)
	{
		if (index >= 0 && index < LOCALES.Length)
		{
			Config?.Set(SettingsManager.GAMEPLAY_LOCALE, LOCALES[index].Locale);
		}
	}

	#endregion
}
