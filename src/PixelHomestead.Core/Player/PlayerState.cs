using PixelHomestead.Core.Core;

namespace PixelHomestead.Core.Player;

public sealed record PlayerState
{
    public GridPosition TilePosition { get; set; } = new(14, 18);
    public float WorldX { get; set; } = 14 * 16;
    public float WorldY { get; set; } = 18 * 16;
    public Direction Facing { get; set; } = Direction.Down;
    public int SelectedHotbarIndex { get; set; }
}
