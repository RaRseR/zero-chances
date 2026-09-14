public sealed class ElevationStep : Step
{
	#region Configuration

	private readonly float SeaThreshold;

	#endregion


	#region Construction

	public ElevationStep(float seaThreshold = 0.5f)
	{
		SeaThreshold = seaThreshold;
	}

	#endregion


	#region Execution

	public override void Execute(StepContext context)
	{
		float[] field = context.Layers.Height;

		float minHeight = context.Configuration.MinHeight;
		float seaLevel = context.Configuration.SeaLevel;
		float maxHeight = context.Configuration.MaxHeight;

		float seaDepth = seaLevel - minHeight;
		float landHeight = maxHeight - seaLevel;

		float aboveSpan = 1f - SeaThreshold;

		for (int index = 0; index < field.Length; index++)
		{
			float value = field[index];

			field[index] = value < SeaThreshold
				? minHeight + value / SeaThreshold * seaDepth
				: seaLevel + (value - SeaThreshold) / aboveSpan * landHeight;
		}
	}

	#endregion
}
