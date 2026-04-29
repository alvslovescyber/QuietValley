namespace QuietValley.Core.Items;

public sealed record InventorySlot
{
    public string? ItemId { get; init; }
    public int Quantity { get; init; }
    public bool IsEmpty => string.IsNullOrWhiteSpace(ItemId) || Quantity <= 0;

    public static InventorySlot Empty { get; } = new();

    public InventorySlot WithQuantity(int quantity)
    {
        return quantity <= 0 ? Empty : this with { Quantity = quantity };
    }
}
