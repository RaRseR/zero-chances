using System;
using System.Collections.Generic;

public static class Planner
{
	#region Planning

	public static Step[] Plan(SessionConfiguration configuration)
	{
		List<Step> steps = new();

		steps.AddRange(CreateShapeStep(configuration.ShapeFeature, configuration.Rand));

		steps.Add(new ClampStep(0f, 1f));

		steps.Add(new EdgeFalloffStep());

		steps.Add(new ElevationStep());

		return steps.ToArray();
	}

	#endregion


	#region Shape

	private static Step[] CreateShapeStep(WorldShapeFeature shape, Rand rand)
	{
		return shape switch
		{
			ContinentFeature => CreateContinentSteps(rand),
			IslandFeature => CreateIslandSteps(rand),
			ArchipelagoFeature => CreateArchipelagoSteps(rand),
			PangeaFeature => CreatePangeaSteps(rand),
			_ => throw new ArgumentOutOfRangeException(
				nameof(shape),
				$"No shape steps for {(shape == null ? "null" : shape.GetType().Name)}"
			)
		};
	}


	private static Step[] CreateContinentSteps(Rand rand)
	{
		List<Step> steps = new();

		float centerX = rand.RangeFloat(0.40f, 0.60f);
		float centerY = rand.RangeFloat(0.40f, 0.60f);

		steps.Add(new RadialStep(
			1f, 1f, centerX, centerY, centerX, centerY, 0.36f, 0.46f, flatFraction: 0.18f, mode: BlendMode.Max
		));

		int satellites = rand.RangeInt(3, 5);

		for (int index = 0; index < satellites; index++)
		{
			float positionX = Math.Clamp(centerX + rand.RangeFloat(-0.28f, 0.28f), 0.12f, 0.88f);
			float positionY = Math.Clamp(centerY + rand.RangeFloat(-0.28f, 0.28f), 0.12f, 0.88f);

			steps.Add(new RadialStep(
				0.55f, 0.85f, positionX, positionY, positionX, positionY, 0.08f, 0.18f,
				flatFraction: 0.12f, mode: BlendMode.Max
			));
		}

		return steps.ToArray();
	}


	private static Step[] CreateIslandSteps(Rand rand)
	{
		List<Step> steps = new();

		float centerX = rand.RangeFloat(0.44f, 0.56f);
		float centerY = rand.RangeFloat(0.44f, 0.56f);

		steps.Add(new RadialStep(
			1f, 1f, centerX, centerY, centerX, centerY, 0.24f, 0.32f, flatFraction: 0.10f, mode: BlendMode.Max
		));

		int satellites = rand.RangeInt(2, 4);

		for (int index = 0; index < satellites; index++)
		{
			float angle = rand.RangeFloat(0f, MathF.Tau);
			float distance = rand.RangeFloat(0.18f, 0.32f);

			float positionX = Math.Clamp(centerX + MathF.Cos(angle) * distance, 0.14f, 0.86f);
			float positionY = Math.Clamp(centerY + MathF.Sin(angle) * distance, 0.14f, 0.86f);

			steps.Add(new RadialStep(
				0.50f, 0.80f, positionX, positionY, positionX, positionY, 0.04f, 0.09f,
				flatFraction: 0.05f, mode: BlendMode.Max
			));
		}

		return steps.ToArray();
	}


	private static Step[] CreateArchipelagoSteps(Rand rand)
	{
		List<Step> steps = new();

		int islands = rand.RangeInt(14, 26);

		for (int index = 0; index < islands; index++)
		{
			float flatFraction = rand.RangeFloat(0.02f, 0.14f);

			steps.Add(new RadialStep(
				0.70f, 1.00f, 0.14f, 0.14f, 0.86f, 0.86f, 0.035f, 0.10f,
				flatFraction: flatFraction, mode: BlendMode.Max
			));
		}

		return steps.ToArray();
	}


	private static Step[] CreatePangeaSteps(Rand rand)
	{
		List<Step> steps = new();

		float centerX = rand.RangeFloat(0.46f, 0.54f);
		float centerY = rand.RangeFloat(0.46f, 0.54f);

		steps.Add(new RadialStep(
			1f, 1f, centerX, centerY, centerX, centerY, 0.52f, 0.62f, flatFraction: 0.28f, mode: BlendMode.Max
		));

		int lobes = rand.RangeInt(4, 6);

		for (int index = 0; index < lobes; index++)
		{
			float angle = rand.RangeFloat(0f, MathF.Tau);
			float distance = rand.RangeFloat(0.22f, 0.42f);

			float positionX = Math.Clamp(centerX + MathF.Cos(angle) * distance, 0.10f, 0.90f);
			float positionY = Math.Clamp(centerY + MathF.Sin(angle) * distance, 0.10f, 0.90f);

			steps.Add(new RadialStep(
				0.60f, 0.90f, positionX, positionY, positionX, positionY, 0.14f, 0.26f,
				flatFraction: 0.16f, mode: BlendMode.Max
			));
		}

		return steps.ToArray();
	}

	#endregion
}
