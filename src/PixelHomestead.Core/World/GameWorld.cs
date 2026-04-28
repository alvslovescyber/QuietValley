using PixelHomestead.Core.Core;

namespace PixelHomestead.Core.World;

public sealed class GameWorld
{
    private readonly Tile[,] _tiles;
    private readonly Dictionary<GridPosition, CropState> _crops = new();

    public GameWorld(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "World dimensions must be positive.");
        }

        Width = width;
        Height = height;
        _tiles = new Tile[width, height];

        for (int tileY = 0; tileY < height; tileY++)
        {
            for (int tileX = 0; tileX < width; tileX++)
            {
                _tiles[tileX, tileY] = new Tile { Type = TileType.Grass };
            }
        }
    }

    public int Width { get; }
    public int Height { get; }
    public IReadOnlyDictionary<GridPosition, CropState> Crops => _crops;

    public Tile GetTile(GridPosition position)
    {
        return IsInside(position) ? _tiles[position.X, position.Y] : new Tile { Type = TileType.Tree };
    }

    public void SetTile(GridPosition position, TileType tileType)
    {
        if (IsInside(position))
        {
            _tiles[position.X, position.Y] = new Tile { Type = tileType };
        }
    }

    public bool IsInside(GridPosition position)
    {
        return position.X >= 0 && position.Y >= 0 && position.X < Width && position.Y < Height;
    }

    public bool BlocksMovement(GridPosition position)
    {
        return !IsInside(position) || GetTile(position).BlocksMovement;
    }

    public bool HasCrop(GridPosition position)
    {
        return _crops.ContainsKey(position);
    }

    public CropState? GetCrop(GridPosition position)
    {
        return _crops.TryGetValue(position, out CropState? crop) ? crop : null;
    }

    public void SetCrop(GridPosition position, CropState crop)
    {
        _crops[position] = crop;
    }

    public bool RemoveCrop(GridPosition position)
    {
        return _crops.Remove(position);
    }

    public IEnumerable<(GridPosition Position, Tile Tile)> Tiles()
    {
        for (int tileY = 0; tileY < Height; tileY++)
        {
            for (int tileX = 0; tileX < Width; tileX++)
            {
                GridPosition position = new(tileX, tileY);
                yield return (position, _tiles[tileX, tileY]);
            }
        }
    }

    public static GameWorld CreateStarterWorld()
    {
        GameWorld world = new(52, 36);

        for (int tileY = 0; tileY < world.Height; tileY++)
        {
            for (int tileX = 0; tileX < world.Width; tileX++)
            {
                if (tileX <= 1 || tileY <= 1 || tileX >= world.Width - 2 || tileY >= world.Height - 2)
                {
                    world.SetTile(new GridPosition(tileX, tileY), TileType.Tree);
                }
            }
        }

        for (int tileY = 7; tileY <= 13; tileY++)
        {
            for (int tileX = 5; tileX <= 13; tileX++)
            {
                world.SetTile(new GridPosition(tileX, tileY), TileType.House);
            }
        }

        world.SetTile(new GridPosition(9, 14), TileType.SleepSpot);

        for (int tileY = 17; tileY <= 26; tileY++)
        {
            for (int tileX = 7; tileX <= 18; tileX++)
            {
                world.SetTile(new GridPosition(tileX, tileY), TileType.Dirt);
            }
        }

        for (int tileY = 9; tileY <= 23; tileY++)
        {
            for (int tileX = 30; tileX <= 43; tileX++)
            {
                double normalized = Math.Pow((tileX - 36.5) / 7.0, 2) + Math.Pow((tileY - 16.0) / 7.0, 2);
                if (normalized < 1.0)
                {
                    world.SetTile(new GridPosition(tileX, tileY), TileType.Water);
                }
            }
        }

        for (int tileX = 10; tileX <= 38; tileX++)
        {
            world.SetTile(new GridPosition(tileX, 15), TileType.Path);
        }

        for (int tileY = 14; tileY <= 30; tileY++)
        {
            world.SetTile(new GridPosition(23, tileY), TileType.Path);
        }

        for (int tileX = 6; tileX <= 19; tileX++)
        {
            world.SetTile(new GridPosition(tileX, 16), TileType.Fence);
            world.SetTile(new GridPosition(tileX, 27), TileType.Fence);
        }

        for (int tileY = 17; tileY <= 26; tileY++)
        {
            world.SetTile(new GridPosition(6, tileY), TileType.Fence);
            world.SetTile(new GridPosition(19, tileY), TileType.Fence);
        }

        world.SetTile(new GridPosition(13, 16), TileType.Path);
        world.SetTile(new GridPosition(13, 27), TileType.Path);
        world.SetTile(new GridPosition(21, 17), TileType.ShippingBox);

        GridPosition[] bushes =
        [
            new(4, 24),
            new(25, 8),
            new(27, 9),
            new(45, 10),
            new(47, 11),
            new(31, 27),
            new(41, 27),
            new(45, 25),
            new(15, 6),
            new(17, 7),
            new(26, 29),
            new(33, 30),
            new(28, 5),
            new(29, 5),
            new(30, 5),
            new(41, 5),
            new(42, 5),
            new(43, 5),
        ];

        foreach (GridPosition decoration in bushes)
        {
            world.SetTile(decoration, TileType.Bush);
        }

        GridPosition[] flowers =
        [
            new(22, 8),
            new(23, 8),
            new(24, 9),
            new(44, 8),
            new(46, 8),
            new(29, 26),
            new(32, 28),
            new(42, 25),
            new(44, 24),
            new(14, 29),
            new(15, 29),
            new(5, 20),
        ];

        foreach (GridPosition flower in flowers)
        {
            world.SetTile(flower, TileType.Flower);
        }

        GridPosition[] tallGrass =
        [
            new(3, 25),
            new(4, 25),
            new(5, 25),
            new(24, 6),
            new(25, 6),
            new(31, 6),
            new(32, 6),
            new(38, 25),
            new(39, 25),
            new(40, 25),
            new(47, 23),
            new(48, 23),
            new(26, 23),
        ];

        foreach (GridPosition grass in tallGrass)
        {
            world.SetTile(grass, TileType.TallGrass);
        }

        GridPosition[] mushrooms = [new(43, 21), new(46, 20), new(45, 24)];
        foreach (GridPosition mushroom in mushrooms)
        {
            world.SetTile(mushroom, TileType.Mushroom);
        }

        GridPosition[] stones = [new(27, 15), new(28, 14), new(29, 13), new(41, 12), new(42, 12), new(22, 26)];
        foreach (GridPosition stone in stones)
        {
            world.SetTile(stone, TileType.Stone);
        }

        world.SetTile(new GridPosition(18, 14), TileType.Barrel);
        world.SetTile(new GridPosition(19, 14), TileType.Barrel);

        return world;
    }
}
