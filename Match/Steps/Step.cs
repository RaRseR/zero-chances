using System;

public abstract class Step
{
	#region Execution

	public abstract void Execute(StepContext context);

	#endregion
}


public sealed class StepContext
{
	#region State

	public readonly WorldLayers Layers;

	public readonly SessionConfiguration Configuration;

	#endregion


	#region Construction

	public StepContext(WorldLayers layers, SessionConfiguration configuration)
	{
		Layers = layers ?? throw new ArgumentNullException(nameof(layers));
		Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
	}

	#endregion


	#region Access

	public Rand Rand => Configuration.Rand;

	public int Side => Layers.Side;

	#endregion
}
