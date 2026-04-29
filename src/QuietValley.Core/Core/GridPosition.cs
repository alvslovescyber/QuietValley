namespace QuietValley.Core.Core;

public readonly record struct GridPosition(int X, int Y)
{
    public static GridPosition operator +(GridPosition position, GridPosition offset)
    {
        return new GridPosition(position.X + offset.X, position.Y + offset.Y);
    }

    public GridPosition Neighbor(Direction direction)
    {
        return direction switch
        {
            Direction.Up => this + new GridPosition(0, -1),
            Direction.Down => this + new GridPosition(0, 1),
            Direction.Left => this + new GridPosition(-1, 0),
            Direction.Right => this + new GridPosition(1, 0),
            _ => this,
        };
    }
}
