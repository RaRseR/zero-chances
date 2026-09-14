using System;

public sealed class EdgeFalloffStep : Step
{
	#region Constants

	private const float MARGIN = 0.10f;

	#endregion


	#region Execution

	public override void Execute(StepContext context)
	{
		float[] field = context.Layers.Height;
		int side = context.Side;

		for (int y = 0; y < side; y++)
		{
			int row = y * side;

			float normalizedY = y / (float)(side - 1);
			float edgeY = MathF.Min(normalizedY, 1f - normalizedY);

			for (int x = 0; x < side; x++)
			{
				float normalizedX = x / (float)(side - 1);
				float distance = MathF.Min(MathF.Min(normalizedX, 1f - normalizedX), edgeY);

				if (distance >= MARGIN)
				{
					continue;
				}

				float t = distance / MARGIN;

				field[row + x] *= t * t * (3f - 2f * t);
			}
		}
	}

	#endregion
}
