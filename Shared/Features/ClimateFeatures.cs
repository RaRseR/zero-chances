public abstract record WorldClimateFeature : WorldFeature;

public sealed record TemperateClimateFeature : WorldClimateFeature;

public sealed record HotClimateFeature : WorldClimateFeature;

public sealed record ColdClimateFeature : WorldClimateFeature;

public sealed record LatitudinalClimateFeature : WorldClimateFeature
{
	public override int MinimumChunkCount => 50;
}
