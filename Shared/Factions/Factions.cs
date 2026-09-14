using Godot;
using System.Collections.Generic;

public static class Factions
{
	#region Constants

	public const int COUNT = 32;

	private const float BORDER_SHADE = 0.55f;

	private static readonly Faction FALLBACK = new Faction("None", "3a3a44", "f2f2f7");

	private static readonly Faction[] ALL =
	{
		new Faction("Boar", "ac3232", "f7dddd"),
		new Faction("Stag", "8f563b", "f7e9e2"),
		new Faction("Fox", "df7126", "2d1200"),
		new Faction("Ram", "eec39a", "302318"),
		new Faction("Scorpion", "d9a066", "2b1c0d"),
		new Faction("Camel", "9c8b76", "1f1a13"),
		new Faction("Wasp", "e8b41c", "2e2300"),
		new Faction("Lion", "fbf236", "323000"),
		new Faction("Tortoise", "8f974a", "1c1e09"),
		new Faction("Mantis", "99e550", "1a2e07"),
		new Faction("Toad", "4b692f", "edf7e3"),
		new Faction("Chameleon", "6abe30", "102602"),
		new Faction("Viper", "37946e", "051e14"),
		new Faction("Jellyfish", "a5e8d5", "1a2e29"),
		new Faction("Dragonfly", "2fd6b0", "002b21"),
		new Faction("Anglerfish", "173b3f", "e0f5f7"),
		new Faction("Peacock", "1f6b78", "dcf3f7"),
		new Faction("Kingfisher", "5fcde4", "0a282e"),
		new Faction("Marlin", "306082", "e0eef7"),
		new Faction("Swallow", "639bff", "0a1933"),
		new Faction("Owl", "cbdbfc", "222732"),
		new Faction("Whale", "4a5cc9", "e0e3f7"),
		new Faction("Moth", "3f3f74", "e6e6f7"),
		new Faction("Snail", "a48ad4", "1d152a"),
		new Faction("Octopus", "76428a", "f2e4f7"),
		new Faction("Flamingo", "d77bba", "2b1223"),
		new Faction("Axolotl", "a82468", "f7dae9"),
		new Faction("Crab", "d95763", "2b090c"),
		new Faction("Shark", "9badb7", "1a2125"),
		new Faction("Rhino", "847e87", "19161b"),
		new Faction("Bear", "595652", "f7f6f4"),
		new Faction("Raven", "2f2b3a", "f0eef7")
	};

	#endregion


	#region Construction

	static Factions()
	{
		for (int index = 0; index < ALL.Length; index++)
		{
			ALL[index].Index = index;
		}
	}

	#endregion


	#region Access

	public static Color Shade(Color color)
	{
		return color.Darkened(BORDER_SHADE);
	}


	public static Faction Get(int index)
	{
		return index >= 0 && index < ALL.Length ? ALL[index] : FALLBACK;
	}


	public static int PickFree(ICollection<int> used, Rand rand)
	{
		List<int> free = new();

		for (int index = 0; index < ALL.Length; index++)
		{
			if (!used.Contains(index))
			{
				free.Add(index);
			}
		}

		return free.Count > 0 ? free[rand.RangeInt(0, free.Count - 1)] : -1;
	}

	#endregion
}
