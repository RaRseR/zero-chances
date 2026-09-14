using System;

public sealed class MatchState
{
	#region State

	public readonly SessionConfiguration Configuration;

	public readonly WorldLayers Layers;

	public int Turn;

	#endregion


	#region Construction

	public MatchState(SessionConfiguration configuration, WorldLayers layers)
	{
		Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		Layers = layers ?? throw new ArgumentNullException(nameof(layers));
		Turn = 0;
	}

	#endregion
}
