using System;

public sealed class ClampStep : Step
{
	#region Configuration

	private readonly float Minimum;
	private readonly float Maximum;

	#endregion


	#region Construction

	public ClampStep(float minimum, float maximum)
	{
		Minimum = minimum;
		Maximum = maximum;
	}

	#endregion


	#region Execution

	public override void Execute(StepContext context)
	{
		float[] field = context.Layers.Height;

		for (int index = 0; index < field.Length; index++)
		{
			field[index] = Math.Clamp(field[index], Minimum, Maximum);
		}
	}

	#endregion
}
