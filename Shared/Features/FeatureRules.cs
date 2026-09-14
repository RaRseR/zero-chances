using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public static class FeatureRules
{
	#region Tables

	private static readonly (Type first, Type second)[] CROSS_CONFLICTS =
	{
	};

	private static readonly Dictionary<Type, Type[]> REQUIRES = new()
	{
	};

	private static Dictionary<string, Type> TypeByName;

	#endregion


	#region Groups

	public static Type GroupOf(WorldFeature feature)
	{
		return feature == null ? null : GroupOf(feature.GetType());
	}


	public static Type GroupOf(Type type)
	{
		while (type != null && type.BaseType != typeof(WorldFeature))
		{
			type = type.BaseType;
		}

		return type;
	}


	public static bool IsSingleChoice(WorldFeature feature)
	{
		return feature is WorldSizeFeature
			|| feature is WorldShapeFeature
			|| feature is WorldClimateFeature
			|| feature is WorldInhabitantFeature;
	}

	#endregion


	#region Conflicts

	public static bool Conflicts(WorldFeature first, WorldFeature second)
	{
		if (first == null || second == null || ReferenceEquals(first, second))
		{
			return false;
		}

		Type firstType = first.GetType();
		Type secondType = second.GetType();

		if (firstType == secondType)
		{
			return false;
		}

		if (IsSingleChoice(first) && GroupOf(first) == GroupOf(second))
		{
			return true;
		}

		foreach ((Type conflictFirst, Type conflictSecond) in CROSS_CONFLICTS)
		{
			bool direct = conflictFirst == firstType && conflictSecond == secondType;
			bool reverse = conflictFirst == secondType && conflictSecond == firstType;

			if (direct || reverse)
			{
				return true;
			}
		}

		return first.Excludes.Contains(secondType) || second.Excludes.Contains(firstType);
	}

	#endregion


	#region Requirements

	public static bool FitsSize(WorldFeature feature, WorldSizeFeature size)
	{
		return feature == null || size == null || size.ChunkCountPerWorldSide >= feature.MinimumChunkCount;
	}


	public static bool IsSatisfied(WorldFeature feature, IReadOnlyList<WorldFeature> chosen)
	{
		if (!REQUIRES.TryGetValue(feature.GetType(), out Type[] options))
		{
			return true;
		}

		foreach (WorldFeature other in chosen)
		{
			if (System.Array.IndexOf(options, other.GetType()) >= 0)
			{
				return true;
			}
		}

		return false;
	}


	public static Type[] MissingFor(WorldFeature feature)
	{
		return REQUIRES.TryGetValue(feature.GetType(), out Type[] options)
			? options
			: System.Array.Empty<Type>();
	}

	#endregion


	#region Creation

	public static WorldFeature Create(string typeName)
	{
		if (string.IsNullOrEmpty(typeName))
		{
			return null;
		}

		TypeByName ??= Assembly
			.GetExecutingAssembly()
			.GetTypes()
			.Where(type => !type.IsAbstract && typeof(WorldFeature).IsAssignableFrom(type))
			.ToDictionary(type => type.Name, type => type);

		return TypeByName.TryGetValue(typeName, out Type resolved)
			? (WorldFeature)Activator.CreateInstance(resolved)
			: null;
	}

	#endregion


	#region Resolution

	public static List<string> Validate(IReadOnlyList<WorldFeature> chosen, WorldSizeFeature size)
	{
		List<string> problems = new();

		if (chosen == null)
		{
			return problems;
		}

		for (int first = 0; first < chosen.Count; first++)
		{
			for (int second = first + 1; second < chosen.Count; second++)
			{
				if (Conflicts(chosen[first], chosen[second]))
				{
					problems.Add($"{Name(chosen[first])} conflicts with {Name(chosen[second])}");
				}
			}
		}

		foreach (WorldFeature feature in chosen)
		{
			if (!FitsSize(feature, size))
			{
				problems.Add($"{Name(feature)} needs a larger world");
			}

			if (IsSatisfied(feature, chosen))
			{
				continue;
			}

			string options = string.Join(" or ", MissingFor(feature).Select(type => Name(type)));

			problems.Add($"{Name(feature)} requires {options}");
		}

		return problems;
	}


	public static string Name(WorldFeature feature)
	{
		return Name(feature.GetType());
	}


	public static string Name(Type type)
	{
		return type.Name
			.Replace("Feature", string.Empty)
			.Replace("Climate", string.Empty)
			.Replace("Size", string.Empty);
	}

	#endregion
}
