using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PixelHomestead.Core.Core;
using PixelHomestead.Core.Items;
using PixelHomestead.Core.World;

namespace PixelHomestead.Game.Rendering;

public sealed class ArtAssets : IDisposable
{
    public ArtAssets(GraphicsDevice graphicsDevice)
    {
        string assetDirectory = Path.Combine(AppContext.BaseDirectory, "Assets", "Generated");
        Terrain = LoadTexture(graphicsDevice, Path.Combine(assetDirectory, "terrain.png"));
        Props = LoadTexture(graphicsDevice, Path.Combine(assetDirectory, "props.png"));
        Player = LoadTexture(graphicsDevice, Path.Combine(assetDirectory, "player.png"));
        Icons = LoadTexture(graphicsDevice, Path.Combine(assetDirectory, "icons.png"));
        Ui = LoadTexture(graphicsDevice, Path.Combine(assetDirectory, "ui.png"));
    }

    public Texture2D Terrain { get; }
    public Texture2D Props { get; }
    public Texture2D Player { get; }
    public Texture2D Icons { get; }
    public Texture2D Ui { get; }

    public static Rectangle TerrainSource(TileType tileType, int variant, int waterFrame)
    {
        int index = tileType switch
        {
            TileType.Grass => Math.Abs(variant) % 4,
            TileType.TallGrass => 4,
            TileType.Flower => 5,
            TileType.Path => 6,
            TileType.Dirt => 7,
            TileType.Soil => 8,
            TileType.Water => 9 + Math.Abs(waterFrame) % 4,
            _ => Math.Abs(variant) % 4,
        };

        return Cell(index);
    }

    public static Rectangle IconSource(ItemDefinition item)
    {
        int index = item.ToolKind switch
        {
            ToolKind.Hoe => 0,
            ToolKind.WateringCan => 1,
            ToolKind.Axe => 2,
            ToolKind.Pickaxe => 3,
            ToolKind.FishingRod => 4,
            _ => item.Id switch
            {
                "turnip_seed" => 5,
                "carrot_seed" => 6,
                "tomato_seed" => 7,
                "potato_seed" => 8,
                "turnip" => 9,
                "carrot" => 10,
                "tomato" => 11,
                "potato" => 12,
                "golden_fish" => 15,
                "pond_carp" => 14,
                _ => item.Type == ItemType.Fish ? 13 : 0,
            },
        };

        return Cell(index);
    }

    public static Rectangle PlayerSource(Direction direction, int frame)
    {
        int row = direction switch
        {
            Direction.Down => 0,
            Direction.Up => 1,
            Direction.Left => 2,
            Direction.Right => 3,
            _ => 0,
        };
        return new Rectangle((Math.Abs(frame) % 4) * 24, row * 16, 24, 16);
    }

    public static Rectangle TreeSource => new(0, 0, 32, 48);
    public static Rectangle BushSource => new(40, 10, 24, 20);
    public static Rectangle MushroomSource => new(72, 8, 32, 36);
    public static Rectangle BarrelSource => new(112, 12, 16, 20);
    public static Rectangle CrateSource => new(136, 12, 16, 20);
    public static Rectangle FenceSource => new(160, 14, 24, 18);
    public static Rectangle ShippingBoxSource => new(192, 12, 20, 20);
    public static Rectangle HouseSource => new(0, 48, 112, 96);

    private static Rectangle Cell(int index)
    {
        return new Rectangle(index % 8 * 16, index / 8 * 16, 16, 16);
    }

    private static Texture2D LoadTexture(GraphicsDevice graphicsDevice, string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Generated art asset is missing: {path}", path);
        }

        using FileStream stream = File.OpenRead(path);
        return Texture2D.FromStream(graphicsDevice, stream);
    }

    public void Dispose()
    {
        Terrain.Dispose();
        Props.Dispose();
        Player.Dispose();
        Icons.Dispose();
        Ui.Dispose();
    }
}
