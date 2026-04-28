using PixelHomestead.Core.Core;
using PixelHomestead.Core.Data;
using PixelHomestead.Core.Energy;
using PixelHomestead.Core.Items;
using PixelHomestead.Core.Systems;
using PixelHomestead.Core.Time;
using PixelHomestead.Core.World;
using Xunit;

namespace PixelHomestead.SmokeTests;

public sealed class TimeEconomyFishingTests
{
    [Fact]
    public void TimeSystem_AdvancesMinutesWrapsAndSleepsToMorning()
    {
        TimeSystem time = new();

        time.Update(61.2);
        Assert.Equal("7:01 AM", time.ClockText);

        time.SetState(3, "Spring", 23 * 60 + 59);
        time.Update(1.0);
        Assert.Equal("12:00 AM", time.ClockText);
        Assert.Equal(0, time.MinutesSinceMidnight);

        time.AdvanceDay();
        Assert.Equal(4, time.Day);
        Assert.Equal("6:00 AM", time.ClockText);
    }

    [Fact]
    public void Economy_ShippingSellsItemsOnSleepAndClearsBin()
    {
        ContentDatabase content = LoadContent();
        GameState state = new(content);
        state.Inventory[8] = new InventorySlot { ItemId = "turnip", Quantity = 2 };

        Assert.True(state.Economy.ShipFromInventory(state.Inventory, 8, content));
        Assert.Single(state.Economy.ShippingBin);

        state.Sleep();

        Assert.Equal(290, state.Economy.Coins);
        Assert.Empty(state.Economy.ShippingBin);
        Assert.True(state.Inventory[8].IsEmpty);
    }

    [Fact]
    public void Fishing_CatchesOnlyAtWaterAndConsumesEnergy()
    {
        ContentDatabase content = LoadContent();
        GameState state = new(content);
        GridPosition water = new(28, 24);
        int startingEnergy = state.Energy.CurrentEnergy;

        string? caught = state.Fishing.TryCatchFishAtWater(state.World, water, state.Inventory, content, state.Energy);

        Assert.NotNull(caught);
        Assert.Equal(startingEnergy - 3, state.Energy.CurrentEnergy);
        Assert.Contains(state.Inventory.Slots, slot => slot.ItemId == caught && slot.Quantity >= 1);

        state.Energy.SetCurrent(2);
        Assert.Null(state.Fishing.TryCatchFishAtWater(state.World, water, state.Inventory, content, state.Energy));
        Assert.Equal(2, state.Energy.CurrentEnergy);
        Assert.Null(
            state.Fishing.TryCatchFishAtWater(
                state.World,
                new GridPosition(14, 18),
                state.Inventory,
                content,
                state.Energy
            )
        );
    }

    [Fact]
    public void Fishing_DoesNotReportCatchWhenInventoryIsFull()
    {
        ContentDatabase content = LoadContent();
        GameState state = new(content);
        GridPosition water = new(28, 24);
        for (int slotIndex = 0; slotIndex < state.Inventory.Capacity; slotIndex++)
        {
            state.Inventory[slotIndex] = new InventorySlot { ItemId = "hoe", Quantity = 1 };
        }

        int startingEnergy = state.Energy.CurrentEnergy;

        Assert.Null(state.Fishing.TryCatchFishAtWater(state.World, water, state.Inventory, content, state.Energy));
        Assert.Equal(startingEnergy, state.Energy.CurrentEnergy);
    }

    [Fact]
    public void Energy_ClampRestoreAndRejectOverspend()
    {
        EnergySystem energy = new();

        energy.SetCurrent(500);
        Assert.Equal(EnergySystem.MaximumEnergy, energy.CurrentEnergy);
        Assert.False(energy.Spend(EnergySystem.MaximumEnergy + 1));
        Assert.Equal(EnergySystem.MaximumEnergy, energy.CurrentEnergy);
        energy.SetCurrent(-10);
        Assert.Equal(0, energy.CurrentEnergy);
        energy.Restore();
        Assert.Equal(EnergySystem.MaximumEnergy, energy.CurrentEnergy);
    }

    private static ContentDatabase LoadContent()
    {
        string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        return ContentDatabase.Load(dataDirectory);
    }
}
