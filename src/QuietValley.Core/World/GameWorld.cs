using QuietValley.Core.Core;

namespace QuietValley.Core.World;

public sealed class GameWorld
{
    private readonly Tile[,] _tiles;
    private readonly Dictionary<GridPosition, Tile> _tileOverrides = new();
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
    public bool IsInfinite => true;
    public IReadOnlyDictionary<GridPosition, CropState> Crops => _crops;

    public Tile GetTile(GridPosition position)
    {
        if (_tileOverrides.TryGetValue(position, out Tile? overrideTile))
        {
            return overrideTile;
        }

        return IsInside(position) ? _tiles[position.X, position.Y] : GenerateProceduralTile(position);
    }

    public void SetTile(GridPosition position, TileType tileType)
    {
        if (IsInside(position))
        {
            _tiles[position.X, position.Y] = new Tile { Type = tileType };
            _tileOverrides.Remove(position);
            return;
        }

        _tileOverrides[position] = new Tile { Type = tileType };
    }

    public bool IsInside(GridPosition position)
    {
        return position.X >= 0 && position.Y >= 0 && position.X < Width && position.Y < Height;
    }

    public bool BlocksMovement(GridPosition position)
    {
        return GetTile(position).BlocksMovement;
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

        foreach ((GridPosition position, Tile tile) in _tileOverrides)
        {
            if (!IsInside(position))
            {
                yield return (position, tile);
            }
        }
    }

    public IEnumerable<(GridPosition Position, Tile Tile)> TilesIn(int minX, int minY, int maxX, int maxY)
    {
        for (int tileY = minY; tileY <= maxY; tileY++)
        {
            for (int tileX = minX; tileX <= maxX; tileX++)
            {
                GridPosition position = new(tileX, tileY);
                yield return (position, GetTile(position));
            }
        }
    }

    public static GameWorld CreateStarterWorld()
    {
        GameWorld world = new(60, 42);

        for (int tileY = 0; tileY < world.Height; tileY++)
        {
            for (int tileX = 0; tileX < world.Width; tileX++)
            {
                GridPosition position = new(tileX, tileY);
                bool upperForestPatch = tileY <= 6 && tileX is > 20 and < 53 && (tileX + tileY) % 3 != 0;
                bool lowerForestPatch = tileY >= 32 && tileX is > 27 and < 56 && (tileX + tileY) % 4 != 1;
                if (upperForestPatch || lowerForestPatch)
                {
                    world.SetTile(position, TileType.Tree);
                }
                else if ((tileX * 17 + tileY * 23) % 31 == 0)
                {
                    world.SetTile(position, TileType.TallGrass);
                }
                else if ((tileX * 13 + tileY * 29) % 43 == 0)
                {
                    world.SetTile(position, TileType.Flower);
                }
            }
        }

        for (int tileY = 5; tileY <= 12; tileY++)
        {
            for (int tileX = 4; tileX <= 11; tileX++)
            {
                world.SetTile(new GridPosition(tileX, tileY), TileType.House);
            }
        }

        world.SetTile(new GridPosition(10, 14), TileType.SleepSpot);

        for (int tileY = 17; tileY <= 27; tileY++)
        {
            for (int tileX = 6; tileX <= 19; tileX++)
            {
                bool cutCorner = (tileX < 8 && tileY < 19) || (tileX > 17 && tileY > 25) || (tileX < 8 && tileY > 25);
                if (!cutCorner)
                {
                    world.SetTile(new GridPosition(tileX, tileY), TileType.Dirt);
                }
            }
        }

        for (int tileY = 8; tileY <= 26; tileY++)
        {
            for (int tileX = 27; tileX <= 49; tileX++)
            {
                double normalized = Math.Pow((tileX - 38.2) / 9.7, 2) + Math.Pow((tileY - 17.3) / 7.6, 2);
                double wobble = Math.Sin(tileX * 1.7) * 0.07 + Math.Cos(tileY * 1.3) * 0.08;
                if (normalized + wobble < 1.0)
                {
                    world.SetTile(new GridPosition(tileX, tileY), TileType.Water);
                }
            }
        }

        for (int tileY = 21; tileY <= 28; tileY++)
        {
            for (int tileX = 25; tileX <= 32; tileX++)
            {
                double normalized = Math.Pow((tileX - 28.5) / 3.8, 2) + Math.Pow((tileY - 24.4) / 3.2, 2);
                if (normalized < 1.0)
                {
                    world.SetTile(new GridPosition(tileX, tileY), TileType.Water);
                }
            }
        }

        GridPosition[] path =
        {
            new(10, 15),
            new(11, 15),
            new(12, 15),
            new(13, 15),
            new(14, 15),
            new(15, 15),
            new(16, 15),
            new(17, 15),
            new(18, 15),
            new(19, 15),
            new(20, 15),
            new(21, 16),
            new(22, 16),
            new(23, 17),
            new(24, 17),
            new(25, 17),
            new(26, 17),
            new(27, 16),
            new(28, 16),
            new(29, 16),
            new(30, 15),
            new(31, 15),
            new(32, 14),
            new(33, 14),
            new(34, 14),
            new(23, 18),
            new(23, 19),
            new(22, 20),
            new(22, 21),
            new(22, 22),
            new(22, 23),
            new(21, 24),
            new(21, 25),
            new(20, 26),
            new(19, 27),
            new(18, 28),
            new(17, 29),
        };

        foreach (GridPosition pathTile in path)
        {
            world.SetTile(pathTile, TileType.Path);
        }

        for (int tileX = 5; tileX <= 20; tileX++)
        {
            world.SetTile(new GridPosition(tileX, 16), TileType.Fence);
            world.SetTile(new GridPosition(tileX, 28), TileType.Fence);
        }

        for (int tileY = 17; tileY <= 27; tileY++)
        {
            world.SetTile(new GridPosition(5, tileY), TileType.Fence);
            world.SetTile(new GridPosition(20, tileY), TileType.Fence);
        }

        world.SetTile(new GridPosition(13, 16), TileType.Path);
        world.SetTile(new GridPosition(13, 28), TileType.Path);
        world.SetTile(new GridPosition(22, 17), TileType.ShippingBox);

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
            new(47, 28),
            new(49, 29),
            new(52, 28),
            new(54, 31),
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
            new(48, 27),
            new(50, 27),
            new(53, 29),
            new(39, 29),
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
            new(27, 24),
            new(28, 25),
            new(50, 26),
            new(51, 26),
            new(52, 26),
        ];

        foreach (GridPosition grass in tallGrass)
        {
            world.SetTile(grass, TileType.TallGrass);
        }

        GridPosition[] mushrooms = [new(48, 22), new(51, 21), new(50, 25), new(46, 24)];
        foreach (GridPosition mushroom in mushrooms)
        {
            world.SetTile(mushroom, TileType.Mushroom);
        }

        GridPosition[] stones =
        [
            new(27, 15),
            new(28, 14),
            new(29, 13),
            new(42, 12),
            new(43, 12),
            new(22, 26),
            new(34, 25),
        ];
        foreach (GridPosition stone in stones)
        {
            world.SetTile(stone, TileType.Stone);
        }

        world.SetTile(new GridPosition(18, 14), TileType.Barrel);
        world.SetTile(new GridPosition(19, 14), TileType.Barrel);
        world.SetTile(new GridPosition(21, 14), TileType.Barrel);

        FillRectangle(world, 36, 28, 43, 34, TileType.TownHouse);
        FillRectangle(world, 48, 28, 54, 34, TileType.TownHouse);
        FillRectangle(world, 34, 35, 56, 36, TileType.Path);
        world.SetTile(new GridPosition(33, 34), TileType.Well);
        world.SetTile(new GridPosition(44, 34), TileType.Mailbox);
        world.SetTile(new GridPosition(45, 36), TileType.Signpost);
        world.SetTile(new GridPosition(56, 34), TileType.LampPost);

        return world;
    }

    private static void FillRectangle(GameWorld world, int left, int top, int right, int bottom, TileType tileType)
    {
        for (int tileY = top; tileY <= bottom; tileY++)
        {
            for (int tileX = left; tileX <= right; tileX++)
            {
                world.SetTile(new GridPosition(tileX, tileY), tileType);
            }
        }
    }

    private static Tile GenerateProceduralTile(GridPosition position)
    {
        int chunkX = FloorDiv(position.X, 16);
        int chunkY = FloorDiv(position.Y, 16);
        int localX = Mod(position.X, 16);
        int localY = Mod(position.Y, 16);
        uint chunkHash = Hash(chunkX, chunkY);
        int pondCenterX = 4 + (int)(chunkHash % 9);
        int pondCenterY = 4 + (int)((chunkHash >> 8) % 9);
        int pondRadius = 3 + (int)((chunkHash >> 16) % 4);
        int pondDeltaX = localX - pondCenterX;
        int pondDeltaY = localY - pondCenterY;
        bool hasPond = chunkHash % 5 == 0;
        if (hasPond && pondDeltaX * pondDeltaX + pondDeltaY * pondDeltaY <= pondRadius * pondRadius)
        {
            return new Tile { Type = TileType.Water };
        }

        bool hasVillage = !hasPond && (chunkHash >> 8) % 8 == 0;
        if (hasVillage)
        {
            int houseX = 1 + (int)((chunkHash >> 4) % 8);
            int houseY = 1 + (int)((chunkHash >> 10) % 6);
            int houseW = 4 + (int)((chunkHash >> 16) % 4);
            int houseH = 3 + (int)((chunkHash >> 20) % 3);
            if (localX >= houseX && localX <= houseX + houseW && localY >= houseY && localY <= houseY + houseH)
            {
                return new Tile { Type = TileType.TownHouse };
            }

            int pathY = houseY + houseH + 1;
            if (localY == pathY && pathY < 16)
            {
                return new Tile { Type = TileType.Path };
            }

            return new Tile { Type = TileType.Grass };
        }

        uint tileHash = Hash(position.X, position.Y);
        int roll = (int)(tileHash % 100);
        TileType type =
            roll < 7 ? TileType.Tree
            : roll < 11 ? TileType.Bush
            : roll < 15 ? TileType.TallGrass
            : roll < 18 ? TileType.Flower
            : roll == 42 ? TileType.Stone
            : TileType.Grass;

        return new Tile { Type = type };
    }

    private static int FloorDiv(int value, int divisor)
    {
        int result = value / divisor;
        int remainder = value % divisor;
        return remainder != 0 && ((remainder < 0) != (divisor < 0)) ? result - 1 : result;
    }

    private static int Mod(int value, int divisor)
    {
        int result = value % divisor;
        return result < 0 ? result + Math.Abs(divisor) : result;
    }

    private static uint Hash(int x, int y)
    {
        unchecked
        {
            uint hash = 2166136261;
            hash = (hash ^ (uint)x) * 16777619;
            hash = (hash ^ (uint)y) * 16777619;
            hash ^= hash >> 13;
            hash *= 1274126177;
            hash ^= hash >> 16;
            return hash;
        }
    }
}
