using PixelHomestead.Core.Data;
using PixelHomestead.Core.Items;

namespace PixelHomestead.Core.Economy;

public sealed class EconomySystem
{
    private readonly List<InventorySlot> _shippingBin = [];

    public int Coins { get; private set; } = 250;
    public IReadOnlyList<InventorySlot> ShippingBin => _shippingBin;

    public bool ShipFromInventory(Inventory inventory, int inventorySlotIndex, ContentDatabase content)
    {
        InventorySlot slot = inventory[inventorySlotIndex];
        if (
            slot.IsEmpty
            || slot.ItemId is null
            || !content.Items.TryGetValue(slot.ItemId, out ItemDefinition? item)
            || item.Type == ItemType.Tool
            || item.SellPrice <= 0
        )
        {
            return false;
        }

        _shippingBin.Add(slot);
        inventory[inventorySlotIndex] = InventorySlot.Empty;
        return true;
    }

    public int SellShippedItems(ContentDatabase content)
    {
        int earned = 0;
        foreach (InventorySlot slot in _shippingBin)
        {
            if (slot.ItemId is null || !content.Items.TryGetValue(slot.ItemId, out ItemDefinition? item))
            {
                continue;
            }

            earned += item.SellPrice * slot.Quantity;
        }

        Coins += earned;
        _shippingBin.Clear();
        return earned;
    }

    public void SetCoins(int coins)
    {
        Coins = Math.Max(0, coins);
    }

    public void RestoreShippingBin(IEnumerable<InventorySlot> shippedItems)
    {
        _shippingBin.Clear();
        foreach (InventorySlot slot in shippedItems)
        {
            if (!slot.IsEmpty && slot.ItemId is not null && slot.Quantity > 0)
            {
                _shippingBin.Add(slot);
            }
        }
    }
}
