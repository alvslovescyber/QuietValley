using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using QuietValley.Core.Core;
using QuietValley.Core.Items;
using QuietValley.Core.World;

namespace QuietValley.Game.Rendering;

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
        MenuBackground = LoadTexture(graphicsDevice, Path.Combine(assetDirectory, "menu_background_ai_source.png"));
        InteriorTown = LoadTexture(graphicsDevice, Path.Combine(assetDirectory, "interior_town.png"));
    }

    public Texture2D Terrain { get; }
    public Texture2D Props { get; }
    public Texture2D Player { get; }
    public Texture2D Icons { get; }
    public Texture2D Ui { get; }
    public Texture2D MenuBackground { get; }
    public Texture2D InteriorTown { get; }

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
                "scythe" => 16,
                "hammer" => 17,
                "shovel" => 18,
                "pond_carp" => 14,
                _ => item.Type == ItemType.Fish ? 13 : 0,
            },
        };

        return IconCell(index);
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

    public static Rectangle TreeSource(int variant)
    {
        return new Rectangle(Math.Abs(variant) % 4 * 56, 0, 48, 72);
    }

    public static Rectangle BushSource(int variant)
    {
        return Math.Abs(variant) % 2 == 0 ? new Rectangle(224, 32, 32, 32) : new Rectangle(264, 32, 32, 32);
    }

    public static Rectangle StoneSource => new(304, 36, 36, 28);
    public static Rectangle MushroomSource => new(352, 32, 48, 36);
    public static Rectangle BarrelSource => new(408, 34, 24, 30);
    public static Rectangle CrateSource => new(440, 32, 34, 34);
    public static Rectangle FenceSource => new(0, 76, 64, 28);
    public static Rectangle FenceRoundSource => new(72, 76, 80, 28);
    public static Rectangle ShippingBoxSource => new(480, 32, 32, 32);
    public static Rectangle HouseSource => new(0, 104, 176, 144);
    public static Rectangle LivingRoomSource => new(26, 52, 736, 602);
    public static Rectangle RedTownHouseSource => new(184, 112, 96, 108);
    public static Rectangle YellowTownHouseSource => new(288, 112, 96, 108);
    public static Rectangle WellSource => new(392, 128, 58, 72);
    public static Rectangle MailboxSource => new(456, 150, 32, 54);
    public static Rectangle SignpostSource => new(0, 256, 48, 54);
    public static Rectangle LampPostSource => new(56, 224, 32, 88);
    public static Rectangle WaterRippleSource => new(384, 272, 54, 30);

    public static Rectangle SwimmerSource(int frame)
    {
        return new Rectangle(160 + Math.Abs(frame) % 4 * 56, 264, 48, 40);
    }

    private static Rectangle Cell(int index)
    {
        return new Rectangle(index % 8 * 16, index / 8 * 16, 16, 16);
    }

    private static Rectangle IconCell(int index)
    {
        return new Rectangle(index * 16, 0, 16, 16);
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
        MenuBackground.Dispose();
        InteriorTown.Dispose();
    }
}
