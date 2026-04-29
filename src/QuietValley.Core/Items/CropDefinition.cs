namespace QuietValley.Core.Items;

public sealed record CropDefinition
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string SeedItemId { get; init; }
    public required string HarvestItemId { get; init; }
    public int GrowthDays { get; init; }
    public bool RequiresWater { get; init; } = true;
    public int SellValue { get; init; }
}
