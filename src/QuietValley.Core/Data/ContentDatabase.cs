using System.Text.Json;
using System.Text.Json.Serialization;
using QuietValley.Core.Fishing;
using QuietValley.Core.Items;
using QuietValley.Core.World;

namespace QuietValley.Core.Data;

public sealed class ContentDatabase
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public required IReadOnlyDictionary<string, ItemDefinition> Items { get; init; }
    public required IReadOnlyDictionary<string, CropDefinition> Crops { get; init; }
    public required IReadOnlyDictionary<string, ToolDefinition> Tools { get; init; }
    public required IReadOnlyDictionary<string, FishDefinition> Fish { get; init; }
    public required IReadOnlyDictionary<string, TileDefinition> Tiles { get; init; }

    public static ContentDatabase Load(string dataDirectory)
    {
        string itemsPath = Path.Combine(dataDirectory, "items.json");
        string cropsPath = Path.Combine(dataDirectory, "crops.json");
        string toolsPath = Path.Combine(dataDirectory, "tools.json");
        string fishPath = Path.Combine(dataDirectory, "fish.json");
        string tilesPath = Path.Combine(dataDirectory, "tiles.json");

        ItemDefinition[] items = ReadArray<ItemDefinition>(itemsPath);
        CropDefinition[] crops = ReadArray<CropDefinition>(cropsPath);
        ToolDefinition[] tools = ReadArray<ToolDefinition>(toolsPath);
        FishDefinition[] fish = ReadArray<FishDefinition>(fishPath);
        TileDefinition[] tiles = ReadArray<TileDefinition>(tilesPath);

        return new ContentDatabase
        {
            Items = items.ToDictionary(item => item.Id),
            Crops = crops.ToDictionary(crop => crop.Id),
            Tools = tools.ToDictionary(tool => tool.Id),
            Fish = fish.ToDictionary(fishDefinition => fishDefinition.Id),
            Tiles = tiles.ToDictionary(tile => tile.Id),
        };
    }

    public CropDefinition? FindCropBySeed(string seedItemId)
    {
        return Crops.Values.FirstOrDefault(crop => crop.SeedItemId == seedItemId);
    }

    private static T[] ReadArray<T>(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Required data file is missing: {path}", path);
        }

        using FileStream stream = File.OpenRead(path);
        return JsonSerializer.Deserialize<T[]>(stream, SerializerOptions) ?? [];
    }
}
