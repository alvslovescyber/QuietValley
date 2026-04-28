using System.Text.Json.Serialization;

namespace PixelHomestead.Core.Items;

public sealed record ItemDefinition
{
    public required string Id { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ItemType Type { get; init; }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ToolKind ToolKind { get; init; }
    public required string IconKey { get; init; }
    public int MaxStack { get; init; } = 99;
    public int SellPrice { get; init; }
}
