using System.Text.Json;
using System.Text.Json.Serialization;
using PixelHomestead.Core.Items;

namespace PixelHomestead.Core.Data;

public sealed class ContentDatabase
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public required IReadOnlyDictionary<string, ItemDefinition> Items { get; init; }
    public required IReadOnlyDictionary<string, CropDefinition> Crops { get; init; }

    public static ContentDatabase Load(string dataDirectory)
    {
        string itemsPath = Path.Combine(dataDirectory, "items.json");
        string cropsPath = Path.Combine(dataDirectory, "crops.json");

        ItemDefinition[] items = ReadArray<ItemDefinition>(itemsPath);
        CropDefinition[] crops = ReadArray<CropDefinition>(cropsPath);

        return new ContentDatabase
        {
            Items = items.ToDictionary(item => item.Id),
            Crops = crops.ToDictionary(crop => crop.Id),
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
