using Microsoft.Xna.Framework;
using QuietValley.Core.Core;
using QuietValley.Core.Player;
using QuietValley.Core.World;

namespace QuietValley.Game.Player;

public sealed class PlayerController
{
    private const float MaximumSpeed = 72f;
    private const float SprintSpeed = 104f;
    private const float SwimSpeed = 46f;
    private const float Acceleration = 560f;
    private const float Deceleration = 720f;
    private float _dustTimer;

    public Vector2 Position { get; private set; }
    public Vector2 Velocity { get; private set; }
    public Direction Facing { get; private set; } = Direction.Down;
    public bool IsMoving => Velocity.LengthSquared() > 16f;
    public bool IsSwimming { get; private set; }
    public float WalkCycle { get; private set; }
    public float ToolUseTimer { get; private set; }
    public float FishingCastTimer { get; private set; }

    public Vector2 Center => Position + new Vector2(8f, 9f);
    public Vector2 Feet => Position + new Vector2(8f, 15f);
    public Rectangle CollisionBox => new((int)Position.X + 3, (int)Position.Y + 10, 10, 5);

    public void ResetFromState(PlayerState state)
    {
        Position = new Vector2(state.WorldX, state.WorldY);
        Velocity = Vector2.Zero;
        Facing = state.Facing;
        WalkCycle = 0;
        _dustTimer = 0;
    }

    public bool Update(GameWorld world, PlayerState state, Vector2 input, bool sprinting, float deltaSeconds)
    {
        IsSwimming = world.GetTile(WorldToTile(Feet)).IsWater;
        float maximumSpeed =
            IsSwimming ? SwimSpeed
            : sprinting ? SprintSpeed
            : MaximumSpeed;
        if (input.LengthSquared() > 0.001f)
        {
            Facing = DirectionFromInput(input, Facing);
            Velocity = MoveTowards(Velocity, input * maximumSpeed, Acceleration * deltaSeconds);
        }
        else
        {
            Velocity = MoveTowards(Velocity, Vector2.Zero, Deceleration * deltaSeconds);
        }

        MoveWithCollision(world, new Vector2(Velocity.X * deltaSeconds, 0));
        MoveWithCollision(world, new Vector2(0, Velocity.Y * deltaSeconds));

        if (IsMoving)
        {
            WalkCycle += deltaSeconds * 8.5f;
            _dustTimer -= deltaSeconds;
        }
        else
        {
            WalkCycle = 0;
            _dustTimer = 0;
        }

        ToolUseTimer = Math.Max(0, ToolUseTimer - deltaSeconds);
        FishingCastTimer = Math.Max(0, FishingCastTimer - deltaSeconds);

        state.TilePosition = WorldToTile(Center);
        IsSwimming = world.GetTile(WorldToTile(Feet)).IsWater;
        state.WorldX = Position.X;
        state.WorldY = Position.Y;
        state.Facing = Facing;

        if (IsMoving && _dustTimer <= 0)
        {
            _dustTimer = 0.16f;
            TileType footTile = world.GetTile(WorldToTile(Feet)).Type;
            return !IsSwimming && footTile is TileType.Dirt or TileType.Path or TileType.Soil;
        }

        return false;
    }

    public GridPosition InteractionTarget()
    {
        return WorldToTile(Feet).Neighbor(Facing);
    }

    public GridPosition? MouseTarget(Vector2 worldPosition)
    {
        GridPosition playerTile = WorldToTile(Feet);
        GridPosition target = WorldToTile(worldPosition);
        int manhattanDistance = Math.Abs(target.X - playerTile.X) + Math.Abs(target.Y - playerTile.Y);
        if (manhattanDistance == 0)
        {
            return InteractionTarget();
        }

        if (manhattanDistance > 1)
        {
            return null;
        }

        return target;
    }

    public Rectangle InteractionRectangle()
    {
        GridPosition target = InteractionTarget();
        return new Rectangle(
            target.X * GameConstants.TileSize,
            target.Y * GameConstants.TileSize,
            GameConstants.TileSize,
            GameConstants.TileSize
        );
    }

    public void TriggerToolUse(bool fishing)
    {
        ToolUseTimer = 0.18f;
        if (fishing)
        {
            FishingCastTimer = 0.35f;
        }
    }

    private void MoveWithCollision(GameWorld world, Vector2 delta)
    {
        if (delta == Vector2.Zero)
        {
            return;
        }

        Vector2 candidate = Position + delta;
        if (CanOccupy(world, candidate))
        {
            Position = candidate;
            return;
        }

        if (delta.X != 0)
        {
            Velocity = new Vector2(0, Velocity.Y);
        }

        if (delta.Y != 0)
        {
            Velocity = new Vector2(Velocity.X, 0);
        }
    }

    private static bool CanOccupy(GameWorld world, Vector2 position)
    {
        RectangleF feet = new(position.X + 3, position.Y + 10, 10, 5);
        GridPosition topLeft = WorldToTile(new Vector2(feet.Left, feet.Top));
        GridPosition topRight = WorldToTile(new Vector2(RightExclusive(feet), feet.Top));
        GridPosition bottomLeft = WorldToTile(new Vector2(feet.Left, BottomExclusive(feet)));
        GridPosition bottomRight = WorldToTile(new Vector2(RightExclusive(feet), BottomExclusive(feet)));

        return !world.BlocksMovement(topLeft)
            && !world.BlocksMovement(topRight)
            && !world.BlocksMovement(bottomLeft)
            && !world.BlocksMovement(bottomRight);
    }

    private static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDelta)
    {
        Vector2 delta = target - current;
        float length = delta.Length();
        if (length <= maxDelta || length == 0)
        {
            return target;
        }

        return current + delta / length * maxDelta;
    }

    private static Direction DirectionFromInput(Vector2 input, Direction fallback)
    {
        if (MathF.Abs(input.X) > MathF.Abs(input.Y))
        {
            return input.X < 0 ? Direction.Left : Direction.Right;
        }

        if (MathF.Abs(input.Y) > 0.001f)
        {
            return input.Y < 0 ? Direction.Up : Direction.Down;
        }

        return fallback;
    }

    private static GridPosition WorldToTile(Vector2 worldPosition)
    {
        return new GridPosition(
            Math.Clamp((int)MathF.Floor(worldPosition.X / GameConstants.TileSize), 0, int.MaxValue),
            Math.Clamp((int)MathF.Floor(worldPosition.Y / GameConstants.TileSize), 0, int.MaxValue)
        );
    }

    private static float RightExclusive(RectangleF rectangle)
    {
        return rectangle.Right - 0.01f;
    }

    private static float BottomExclusive(RectangleF rectangle)
    {
        return rectangle.Bottom - 0.01f;
    }

    private readonly record struct RectangleF(float X, float Y, float Width, float Height)
    {
        public float Left => X;
        public float Top => Y;
        public float Right => X + Width;
        public float Bottom => Y + Height;
    }
}
