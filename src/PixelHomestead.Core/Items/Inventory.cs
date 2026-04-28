namespace PixelHomestead.Core.Items;

public sealed class Inventory
{
    private readonly List<InventorySlot> _slots;

    public Inventory(int capacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Inventory capacity must be positive.");
        }

        _slots = Enumerable.Repeat(InventorySlot.Empty, capacity).ToList();
    }

    public int Capacity => _slots.Count;
    public IReadOnlyList<InventorySlot> Slots => _slots;

    public InventorySlot this[int index]
    {
        get => _slots[index];
        set => _slots[index] = value;
    }

    public bool Add(string itemId, int quantity, IReadOnlyDictionary<string, ItemDefinition> itemDefinitions)
    {
        if (quantity <= 0)
        {
            return true;
        }

        if (!itemDefinitions.TryGetValue(itemId, out ItemDefinition? itemDefinition))
        {
            throw new InvalidOperationException($"Unknown item id '{itemId}'.");
        }

        int remaining = quantity;

        for (int slotIndex = 0; slotIndex < _slots.Count && remaining > 0; slotIndex++)
        {
            InventorySlot slot = _slots[slotIndex];
            if (slot.ItemId != itemId || slot.Quantity >= itemDefinition.MaxStack)
            {
                continue;
            }

            int added = Math.Min(remaining, itemDefinition.MaxStack - slot.Quantity);
            _slots[slotIndex] = slot with { Quantity = slot.Quantity + added };
            remaining -= added;
        }

        for (int slotIndex = 0; slotIndex < _slots.Count && remaining > 0; slotIndex++)
        {
            if (!_slots[slotIndex].IsEmpty)
            {
                continue;
            }

            int added = Math.Min(remaining, itemDefinition.MaxStack);
            _slots[slotIndex] = new InventorySlot { ItemId = itemId, Quantity = added };
            remaining -= added;
        }

        return remaining == 0;
    }

    public bool Remove(string itemId, int quantity)
    {
        if (quantity <= 0)
        {
            return true;
        }

        int available = _slots.Where(slot => slot.ItemId == itemId).Sum(slot => slot.Quantity);
        if (available < quantity)
        {
            return false;
        }

        int remaining = quantity;
        for (int slotIndex = 0; slotIndex < _slots.Count && remaining > 0; slotIndex++)
        {
            InventorySlot slot = _slots[slotIndex];
            if (slot.ItemId != itemId)
            {
                continue;
            }

            int removed = Math.Min(remaining, slot.Quantity);
            _slots[slotIndex] = slot.WithQuantity(slot.Quantity - removed);
            remaining -= removed;
        }

        return true;
    }

    public void Move(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex)
        {
            return;
        }

        (_slots[fromIndex], _slots[toIndex]) = (_slots[toIndex], _slots[fromIndex]);
    }

    public void Clear()
    {
        for (int slotIndex = 0; slotIndex < _slots.Count; slotIndex++)
        {
            _slots[slotIndex] = InventorySlot.Empty;
        }
    }
}
