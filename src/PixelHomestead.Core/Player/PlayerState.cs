using PixelHomestead.Core.Core;

namespace PixelHomestead.Core.Player;

public sealed record PlayerState
{
    public GridPosition TilePosition { get; set; } = new(14, 18);
    public Direction Facing { get; set; } = Direction.Down;
    public int SelectedHotbarIndex { get; set; }
}
