namespace PixelHomestead.Core.Items;

public sealed record ToolDefinition
{
    public required string Id { get; init; }
    public required string ItemId { get; init; }
    public required string DisplayName { get; init; }
    public int EnergyCost { get; init; }
    public float UseDurationSeconds { get; init; } = 0.18f;
    public required string FeedbackCue { get; init; }
}
