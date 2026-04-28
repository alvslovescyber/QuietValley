using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelHomestead.Core.Core;
using PixelHomestead.Core.Items;
using PixelHomestead.Core.Systems;
using PixelHomestead.Core.World;

namespace PixelHomestead.Game.Rendering;

public sealed class WorldRenderer(ArtAssets art, Texture2D pixel)
{
    public void DrawTerrain(SpriteBatch spriteBatch, GameState state, Vector2 camera, double waterAnimation)
    {
        Rectangle visibleWorld = new(
            (int)camera.X - GameConstants.TileSize * 2,
            (int)camera.Y - GameConstants.TileSize * 4,
            GameConstants.VirtualWidth + GameConstants.TileSize * 4,
            GameConstants.VirtualHeight + GameConstants.TileSize * 8
        );

        int waterFrame = (int)MathF.Floor((float)waterAnimation * 5f);
        foreach ((GridPosition position, Tile tile) in state.World.Tiles())
        {
            Rectangle worldRectangle = TileWorldRectangle(position);
            if (!visibleWorld.Intersects(worldRectangle))
            {
                continue;
            }

            DrawBaseTile(spriteBatch, state.World, position, tile.Type, camera, waterFrame);
        }

        DrawHouse(spriteBatch, camera);
        DrawCrops(spriteBatch, state, camera);
        DrawProps(spriteBatch, state.World, camera);
    }

    public void DrawWaterOverlay(SpriteBatch spriteBatch, GameWorld world, Vector2 camera, double waterAnimation)
    {
        int shimmer = (int)MathF.Floor((float)waterAnimation * 10f);
        foreach ((GridPosition position, Tile tile) in world.Tiles())
        {
            if (tile.Type != TileType.Water || (position.X + position.Y + shimmer) % 5 != 0)
            {
                continue;
            }

            Rectangle rectangle = TileScreenRectangle(position, camera);
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + 4, rectangle.Y + 5, 7, 1),
                new Color(164, 236, 240, 130)
            );
            spriteBatch.Draw(
                pixel,
                new Rectangle(rectangle.X + 9, rectangle.Y + 11, 4, 1),
                new Color(164, 236, 240, 90)
            );
        }
    }

    private void DrawBaseTile(
        SpriteBatch spriteBatch,
        GameWorld world,
        GridPosition position,
        TileType tileType,
        Vector2 camera,
        int waterFrame
    )
    {
        Rectangle destination = TileScreenRectangle(position, camera);
        TileType groundType = tileType
            is TileType.Tree
                or TileType.Bush
                or TileType.Mushroom
                or TileType.Stone
                or TileType.Barrel
                or TileType.Fence
                or TileType.ShippingBox
                or TileType.House
            ? TileType.Grass
            : tileType;

        int variant = Math.Abs(position.X * 19 + position.Y * 37);
        spriteBatch.Draw(
            art.Terrain,
            destination,
            ArtAssets.TerrainSource(groundType, variant, waterFrame + variant),
            Color.White
        );

        if (tileType == TileType.Water)
        {
            DrawWaterEdges(spriteBatch, world, position, destination);
        }

        if (groundType is TileType.Path or TileType.Dirt)
        {
            DrawSoftEdge(spriteBatch, world, position, destination, groundType);
        }
    }

    private void DrawSoftEdge(
        SpriteBatch spriteBatch,
        GameWorld world,
        GridPosition position,
        Rectangle destination,
        TileType groundType
    )
    {
        Color edge = groundType == TileType.Path ? new Color(123, 83, 38, 70) : new Color(68, 50, 32, 75);
        if (world.GetTile(position.Neighbor(Direction.Up)).Type == TileType.Grass)
        {
            spriteBatch.Draw(pixel, new Rectangle(destination.X, destination.Y, destination.Width, 2), edge);
        }

        if (world.GetTile(position.Neighbor(Direction.Down)).Type == TileType.Grass)
        {
            spriteBatch.Draw(pixel, new Rectangle(destination.X, destination.Bottom - 2, destination.Width, 2), edge);
        }

        if (world.GetTile(position.Neighbor(Direction.Left)).Type == TileType.Grass)
        {
            spriteBatch.Draw(pixel, new Rectangle(destination.X, destination.Y, 2, destination.Height), edge);
        }

        if (world.GetTile(position.Neighbor(Direction.Right)).Type == TileType.Grass)
        {
            spriteBatch.Draw(pixel, new Rectangle(destination.Right - 2, destination.Y, 2, destination.Height), edge);
        }
    }

    private void DrawWaterEdges(SpriteBatch spriteBatch, GameWorld world, GridPosition position, Rectangle destination)
    {
        Color bank = new(128, 142, 132);
        Color sand = new(218, 155, 66);
        DrawWaterEdge(
            spriteBatch,
            world,
            position,
            Direction.Up,
            new Rectangle(destination.X, destination.Y, 16, 3),
            bank,
            sand
        );
        DrawWaterEdge(
            spriteBatch,
            world,
            position,
            Direction.Down,
            new Rectangle(destination.X, destination.Bottom - 3, 16, 3),
            bank,
            sand
        );
        DrawWaterEdge(
            spriteBatch,
            world,
            position,
            Direction.Left,
            new Rectangle(destination.X, destination.Y, 3, 16),
            bank,
            sand
        );
        DrawWaterEdge(
            spriteBatch,
            world,
            position,
            Direction.Right,
            new Rectangle(destination.Right - 3, destination.Y, 3, 16),
            bank,
            sand
        );
    }

    private void DrawWaterEdge(
        SpriteBatch spriteBatch,
        GameWorld world,
        GridPosition position,
        Direction direction,
        Rectangle rectangle,
        Color bank,
        Color sand
    )
    {
        if (world.GetTile(position.Neighbor(direction)).Type == TileType.Water)
        {
            return;
        }

        spriteBatch.Draw(pixel, rectangle, bank);
        Rectangle inner = rectangle;
        inner.Inflate(-1, -1);
        if (inner.Width > 0 && inner.Height > 0)
        {
            spriteBatch.Draw(pixel, inner, sand);
        }
    }

    private void DrawHouse(SpriteBatch spriteBatch, Vector2 camera)
    {
        Rectangle destination = new(
            5 * GameConstants.TileSize - (int)camera.X - 6,
            5 * GameConstants.TileSize - (int)camera.Y,
            112,
            96
        );
        DrawShadow(spriteBatch, new Rectangle(destination.X + 8, destination.Y + 82, destination.Width - 16, 10));
        spriteBatch.Draw(art.Props, destination, ArtAssets.HouseSource, Color.White);
    }

    private void DrawCrops(SpriteBatch spriteBatch, GameState state, Vector2 camera)
    {
        foreach ((GridPosition position, CropState cropState) in state.World.Crops)
        {
            Rectangle destination = TileScreenRectangle(position, camera);
            DrawCrop(spriteBatch, destination, cropState, state.Content.Crops[cropState.CropId]);
        }
    }

    private void DrawCrop(SpriteBatch spriteBatch, Rectangle destination, CropState cropState, CropDefinition crop)
    {
        float growthRatio = crop.GrowthDays <= 0 ? 1 : cropState.GrowthProgress / (float)crop.GrowthDays;
        int height =
            growthRatio < 0.34f ? 4
            : growthRatio < 0.67f ? 8
            : 12;
        Color leaf = growthRatio >= 1 ? new Color(42, 152, 67) : new Color(73, 184, 74);
        Color fruit = crop.HarvestItemId switch
        {
            "carrot" => new Color(238, 126, 45),
            "turnip" => new Color(240, 230, 218),
            "potato" => new Color(181, 126, 64),
            _ => new Color(230, 73, 67),
        };

        spriteBatch.Draw(pixel, new Rectangle(destination.X + 5, destination.Y + 13, 7, 2), new Color(40, 27, 18, 85));
        spriteBatch.Draw(pixel, new Rectangle(destination.X + 7, destination.Y + 14 - height, 2, height), leaf);
        spriteBatch.Draw(pixel, new Rectangle(destination.X + 5, destination.Y + 12 - height / 2, 7, 3), leaf);

        if (growthRatio >= 1)
        {
            spriteBatch.Draw(pixel, new Rectangle(destination.X + 6, destination.Y + 7, 5, 5), fruit);
            spriteBatch.Draw(pixel, new Rectangle(destination.X + 7, destination.Y + 8, 1, 1), Palette.ParchmentLight);
        }

        if (cropState.WateredToday)
        {
            spriteBatch.Draw(
                pixel,
                new Rectangle(destination.X + 2, destination.Y + 3, 3, 2),
                new Color(143, 227, 238, 170)
            );
        }
    }

    private void DrawProps(SpriteBatch spriteBatch, GameWorld world, Vector2 camera)
    {
        foreach ((GridPosition position, Tile tile) in world.Tiles().OrderBy(entry => entry.Position.Y))
        {
            Rectangle tileRectangle = TileScreenRectangle(position, camera);
            switch (tile.Type)
            {
                case TileType.Tree:
                    DrawShadow(spriteBatch, new Rectangle(tileRectangle.X - 5, tileRectangle.Y + 12, 26, 7));
                    spriteBatch.Draw(
                        art.Props,
                        new Rectangle(tileRectangle.X - 8, tileRectangle.Y - 32, 32, 48),
                        ArtAssets.TreeSource,
                        Color.White
                    );
                    break;
                case TileType.Bush:
                    DrawShadow(spriteBatch, new Rectangle(tileRectangle.X + 1, tileRectangle.Y + 10, 14, 4));
                    spriteBatch.Draw(
                        art.Props,
                        new Rectangle(tileRectangle.X - 4, tileRectangle.Y - 4, 24, 20),
                        ArtAssets.BushSource,
                        Color.White
                    );
                    break;
                case TileType.Mushroom:
                    DrawShadow(spriteBatch, new Rectangle(tileRectangle.X - 6, tileRectangle.Y + 13, 28, 6));
                    spriteBatch.Draw(
                        art.Props,
                        new Rectangle(tileRectangle.X - 8, tileRectangle.Y - 20, 32, 36),
                        ArtAssets.MushroomSource,
                        Color.White
                    );
                    break;
                case TileType.Stone:
                    DrawShadow(spriteBatch, new Rectangle(tileRectangle.X + 2, tileRectangle.Y + 11, 12, 4));
                    DrawStone(spriteBatch, tileRectangle);
                    break;
                case TileType.Barrel:
                    DrawShadow(spriteBatch, new Rectangle(tileRectangle.X + 2, tileRectangle.Y + 13, 12, 4));
                    spriteBatch.Draw(
                        art.Props,
                        new Rectangle(tileRectangle.X, tileRectangle.Y - 4, 16, 20),
                        ArtAssets.BarrelSource,
                        Color.White
                    );
                    break;
                case TileType.Fence:
                    DrawShadow(spriteBatch, new Rectangle(tileRectangle.X, tileRectangle.Y + 13, 16, 4));
                    spriteBatch.Draw(
                        art.Props,
                        new Rectangle(tileRectangle.X - 4, tileRectangle.Y - 2, 24, 18),
                        ArtAssets.FenceSource,
                        Color.White
                    );
                    break;
                case TileType.ShippingBox:
                    DrawShadow(spriteBatch, new Rectangle(tileRectangle.X + 1, tileRectangle.Y + 13, 15, 4));
                    spriteBatch.Draw(
                        art.Props,
                        new Rectangle(tileRectangle.X - 2, tileRectangle.Y - 3, 20, 20),
                        ArtAssets.ShippingBoxSource,
                        Color.White
                    );
                    break;
                case TileType.SleepSpot:
                    DrawSleepMat(spriteBatch, tileRectangle);
                    break;
            }
        }
    }

    private void DrawSleepMat(SpriteBatch spriteBatch, Rectangle rectangle)
    {
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 1, rectangle.Y + 6, 14, 8), Palette.ParchmentLight);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 1, rectangle.Y + 12, 14, 2), Palette.ParchmentDark);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 3, rectangle.Y + 8, 4, 3), new Color(113, 156, 215));
    }

    private void DrawStone(SpriteBatch spriteBatch, Rectangle rectangle)
    {
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 3, rectangle.Y + 7, 10, 6), new Color(119, 129, 128));
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 5, rectangle.Y + 5, 7, 4), Palette.Rock);
        spriteBatch.Draw(pixel, new Rectangle(rectangle.X + 6, rectangle.Y + 6, 4, 1), new Color(195, 199, 188));
    }

    private void DrawShadow(SpriteBatch spriteBatch, Rectangle rectangle)
    {
        spriteBatch.Draw(pixel, rectangle, new Color(24, 18, 14, 70));
    }

    private static Rectangle TileWorldRectangle(GridPosition position)
    {
        return new Rectangle(
            position.X * GameConstants.TileSize,
            position.Y * GameConstants.TileSize,
            GameConstants.TileSize,
            GameConstants.TileSize
        );
    }

    private static Rectangle TileScreenRectangle(GridPosition position, Vector2 camera)
    {
        return new Rectangle(
            position.X * GameConstants.TileSize - (int)camera.X,
            position.Y * GameConstants.TileSize - (int)camera.Y,
            GameConstants.TileSize,
            GameConstants.TileSize
        );
    }
}
