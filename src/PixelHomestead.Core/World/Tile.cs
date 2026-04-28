namespace PixelHomestead.Core.World;

public sealed record Tile
{
    public TileType Type { get; init; }
    public bool IsWater => Type == TileType.Water;
    public bool BlocksMovement =>
        Type
            is TileType.Tree
                or TileType.Bush
                or TileType.Mushroom
                or TileType.Stone
                or TileType.Barrel
                or TileType.Fence
                or TileType.House
                or TileType.TownHouse
                or TileType.Well
                or TileType.Mailbox
                or TileType.Signpost
                or TileType.LampPost
                or TileType.ShippingBox;
    public bool CanTill => Type is TileType.Dirt or TileType.Grass;
}
