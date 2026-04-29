namespace QuietValley.Core.World;

public sealed record TileDefinition
{
    public required string Id { get; init; }
    public TileType TileType { get; init; }
    public bool BlocksMovement { get; init; }
    public bool NaturalDecoration { get; init; }
    public required string SpriteKey { get; init; }
}
