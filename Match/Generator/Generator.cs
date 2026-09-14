using System.Diagnostics;

public sealed class WorldGeneration
{
	#region State

	public WorldLayers Layers;

	public Step[] Plan;

	public double[] Milliseconds;

	public double TotalMilliseconds;

	public int Seed;

	#endregion
}


public static class Generator
{
	#region Generation

	public static WorldGeneration Generate(SessionConfiguration configuration)
	{
		Stopwatch total = Stopwatch.StartNew();

		WorldLayers layers = new WorldLayers(configuration.PointCountPerWorldSide);

		Step[] plan = Planner.Plan(configuration);

		double[] milliseconds = new double[plan.Length];

		StepContext context = new StepContext(layers, configuration);

		for (int index = 0; index < plan.Length; index++)
		{
			Stopwatch stepWatch = Stopwatch.StartNew();

			plan[index].Execute(context);

			stepWatch.Stop();

			milliseconds[index] = stepWatch.Elapsed.TotalMilliseconds;
		}

		total.Stop();

		return new WorldGeneration
		{
			Layers = layers,
			Plan = plan,
			Milliseconds = milliseconds,
			TotalMilliseconds = total.Elapsed.TotalMilliseconds,
			Seed = configuration.Seed
		};
	}

	#endregion
}
