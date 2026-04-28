using Microsoft.Xna.Framework;
using PixelHomestead.Core.World;

namespace PixelHomestead.Game.Rendering;

public sealed class Camera2D
{
    public Vector2 Position { get; private set; }

    public void SnapTo(Vector2 target, GameWorld world)
    {
        Position = Clamp(target, world);
    }

    public void Follow(Vector2 target, GameWorld world, float deltaSeconds)
    {
        Vector2 desired = target - new Vector2(GameConstants.VirtualWidth / 2f, GameConstants.VirtualHeight / 2f);
        desired = Clamp(desired, world);
        float smoothing = 1f - MathF.Pow(0.001f, deltaSeconds);
        Position = Vector2.Lerp(Position, desired, smoothing);
    }

    private static Vector2 Clamp(Vector2 target, GameWorld world)
    {
        float maxX = Math.Max(0, world.Width * GameConstants.TileSize - GameConstants.VirtualWidth);
        float maxY = Math.Max(0, world.Height * GameConstants.TileSize - GameConstants.VirtualHeight);
        return new Vector2(Math.Clamp(target.X, 0, maxX), Math.Clamp(target.Y, 0, maxY));
    }
}
