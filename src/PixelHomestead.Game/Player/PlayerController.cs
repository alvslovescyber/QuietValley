using Microsoft.Xna.Framework;
using PixelHomestead.Core.Core;
using PixelHomestead.Core.Player;
using PixelHomestead.Core.World;

namespace PixelHomestead.Game.Player;

public sealed class PlayerController
{
    private const float MaximumSpeed = 72f;
    private const float Acceleration = 560f;
    private const float Deceleration = 720f;
    private const float InteractionDistance = 18f;

    private float _dustTimer;

    public Vector2 Position { get; private set; }
    public Vector2 Velocity { get; private set; }
    public Direction Facing { get; private set; } = Direction.Down;
    public bool IsMoving => Velocity.LengthSquared() > 16f;
    public float WalkCycle { get; private set; }
    public float ToolUseTimer { get; private set; }
    public float FishingCastTimer { get; private set; }

    public Vector2 Center => Position + new Vector2(8f, 9f);
    public Vector2 Feet => Position + new Vector2(8f, 15f);

    public void ResetFromState(PlayerState state)
    {
        Position = new Vector2(
            state.TilePosition.X * GameConstants.TileSize,
            state.TilePosition.Y * GameConstants.TileSize
        );
        Velocity = Vector2.Zero;
        Facing = state.Facing;
        WalkCycle = 0;
        _dustTimer = 0;
    }

    public bool Update(GameWorld world, PlayerState state, Vector2 input, float deltaSeconds)
    {
        if (input.LengthSquared() > 0.001f)
        {
            Facing = DirectionFromInput(input, Facing);
            Velocity = MoveTowards(Velocity, input * MaximumSpeed, Acceleration * deltaSeconds);
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
        state.Facing = Facing;

        if (IsMoving && _dustTimer <= 0)
        {
            _dustTimer = 0.16f;
            TileType footTile = world.GetTile(WorldToTile(Feet)).Type;
            return footTile is TileType.Dirt or TileType.Path or TileType.Soil;
        }

        return false;
    }

    public GridPosition InteractionTarget()
    {
        Vector2 offset = Facing switch
        {
            Direction.Up => new Vector2(0, -InteractionDistance),
            Direction.Down => new Vector2(0, InteractionDistance),
            Direction.Left => new Vector2(-InteractionDistance, 0),
            Direction.Right => new Vector2(InteractionDistance, 0),
            _ => Vector2.Zero,
        };

        return WorldToTile(Center + offset);
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
        GridPosition topRight = WorldToTile(new Vector2(feet.Right, feet.Top));
        GridPosition bottomLeft = WorldToTile(new Vector2(feet.Left, feet.Bottom));
        GridPosition bottomRight = WorldToTile(new Vector2(feet.Right, feet.Bottom));

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

    private readonly record struct RectangleF(float X, float Y, float Width, float Height)
    {
        public float Left => X;
        public float Top => Y;
        public float Right => X + Width;
        public float Bottom => Y + Height;
    }
}
