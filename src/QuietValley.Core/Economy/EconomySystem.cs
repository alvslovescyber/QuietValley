using QuietValley.Core.Data;
using QuietValley.Core.Items;

namespace QuietValley.Core.Economy;

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
        long earned = 0;
        foreach (InventorySlot slot in _shippingBin)
        {
            if (
                slot.ItemId is null
                || !content.Items.TryGetValue(slot.ItemId, out ItemDefinition? item)
                || slot.Quantity <= 0
            )
            {
                continue;
            }

            earned += (long)item.SellPrice * slot.Quantity;
        }

        Coins = (int)Math.Min(int.MaxValue, Coins + earned);
        _shippingBin.Clear();
        return (int)Math.Min(int.MaxValue, earned);
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
