public abstract record WorldShapeFeature : WorldFeature;

public sealed record IslandFeature : WorldShapeFeature;

public sealed record ContinentFeature : WorldShapeFeature;

public sealed record ArchipelagoFeature : WorldShapeFeature;

public sealed record PangeaFeature : WorldShapeFeature;
