namespace QuietValley.Core.Fishing;

public sealed record FishDefinition
{
    public required string Id { get; init; }
    public required string ItemId { get; init; }
    public required string DisplayName { get; init; }
    public int RarityWeight { get; init; } = 10;
    public required string WaterType { get; init; }
}
