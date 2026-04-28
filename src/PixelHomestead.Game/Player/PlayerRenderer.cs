using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelHomestead.Game.Rendering;

namespace PixelHomestead.Game.Player;

public sealed class PlayerRenderer(ArtAssets art, Texture2D pixel)
{
    public void Draw(SpriteBatch spriteBatch, PlayerController player, Vector2 camera)
    {
        Vector2 screen = player.Position - camera;
        spriteBatch.Draw(pixel, new Rectangle((int)screen.X + 2, (int)screen.Y + 14, 12, 3), new Color(31, 20, 14, 80));

        int frame = player.IsMoving ? (int)MathF.Floor(player.WalkCycle) % 4 : 0;
        Rectangle source = ArtAssets.PlayerSource(player.Facing, frame);
        int bob = player.IsMoving && frame is 1 or 3 ? -1 : 0;
        Rectangle destination = new((int)screen.X - 4, (int)screen.Y + bob, 24, 16);
        if (player.IsSwimming)
        {
            DrawSwimming(spriteBatch, player, screen, source, destination);
        }
        else
        {
            spriteBatch.Draw(art.Player, destination, source, Color.White);
        }

        if (player.ToolUseTimer > 0)
        {
            DrawToolSwing(spriteBatch, player, screen);
        }

        if (player.FishingCastTimer > 0)
        {
            DrawFishingLine(spriteBatch, player, screen);
        }
    }

    private void DrawSwimming(
        SpriteBatch spriteBatch,
        PlayerController player,
        Vector2 screen,
        Rectangle source,
        Rectangle destination
    )
    {
        Rectangle headSource = new(source.X, source.Y, source.Width, 10);
        Rectangle headDestination = new(destination.X, destination.Y + 2, destination.Width, 10);
        spriteBatch.Draw(art.Player, headDestination, headSource, Color.White);
        spriteBatch.Draw(
            pixel,
            new Rectangle((int)screen.X + 1, (int)screen.Y + 11, 15, 3),
            new Color(102, 205, 232, 185)
        );
        spriteBatch.Draw(
            pixel,
            new Rectangle((int)screen.X + 3, (int)screen.Y + 13, 11, 2),
            new Color(35, 118, 178, 150)
        );

        int bubbleOffset = (int)MathF.Floor(player.WalkCycle) % 10;
        DrawBubble(spriteBatch, (int)screen.X + 16, (int)screen.Y + 6 - bubbleOffset / 2, 3);
        if (bubbleOffset > 4)
        {
            DrawBubble(spriteBatch, (int)screen.X + 20, (int)screen.Y + 2, 2);
        }
    }

    private void DrawBubble(SpriteBatch spriteBatch, int x, int y, int size)
    {
        spriteBatch.Draw(pixel, new Rectangle(x, y, size, size), new Color(220, 250, 255, 180));
        spriteBatch.Draw(
            pixel,
            new Rectangle(x + 1, y + 1, Math.Max(1, size - 2), Math.Max(1, size - 2)),
            new Color(102, 205, 232, 110)
        );
    }

    private void DrawToolSwing(SpriteBatch spriteBatch, PlayerController player, Vector2 screen)
    {
        Color toolColor = new(255, 224, 88, 180);
        Rectangle rectangle = player.Facing switch
        {
            Core.Core.Direction.Up => new((int)screen.X + 6, (int)screen.Y - 5, 2, 8),
            Core.Core.Direction.Down => new((int)screen.X + 8, (int)screen.Y + 10, 2, 8),
            Core.Core.Direction.Left => new((int)screen.X - 4, (int)screen.Y + 8, 8, 2),
            Core.Core.Direction.Right => new((int)screen.X + 12, (int)screen.Y + 8, 8, 2),
            _ => new((int)screen.X + 8, (int)screen.Y + 10, 2, 8),
        };
        spriteBatch.Draw(pixel, rectangle, toolColor);
    }

    private void DrawFishingLine(SpriteBatch spriteBatch, PlayerController player, Vector2 screen)
    {
        Vector2 start = screen + new Vector2(9, 9);
        Vector2 end =
            start
            + player.Facing switch
            {
                Core.Core.Direction.Up => new Vector2(0, -22),
                Core.Core.Direction.Down => new Vector2(0, 22),
                Core.Core.Direction.Left => new Vector2(-26, 0),
                Core.Core.Direction.Right => new Vector2(26, 0),
                _ => new Vector2(0, 22),
            };

        DrawLine(spriteBatch, start, end, new Color(255, 241, 190, 190));
        spriteBatch.Draw(pixel, new Rectangle((int)end.X - 2, (int)end.Y - 2, 4, 4), new Color(255, 255, 255, 210));
    }

    private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color)
    {
        Vector2 delta = end - start;
        int steps = Math.Max(1, (int)delta.Length());
        for (int step = 0; step <= steps; step++)
        {
            Vector2 point = Vector2.Lerp(start, end, step / (float)steps);
            spriteBatch.Draw(pixel, new Rectangle((int)point.X, (int)point.Y, 1, 1), color);
        }
    }
}
