using PixelHomestead.Core.Core;
using PixelHomestead.Core.Data;
using PixelHomestead.Core.Items;
using PixelHomestead.Core.Systems;
using PixelHomestead.Core.World;
using Xunit;

namespace PixelHomestead.SmokeTests;

public sealed class FarmingSystemTests
{
    [Fact]
    public void StarterState_HasExpectedToolsAndWorld()
    {
        GameState state = CreateState();

        Assert.All(state.Inventory.Slots.Take(5), slot => Assert.False(slot.IsEmpty));
        Assert.Equal(TileType.Dirt, state.World.GetTile(new GridPosition(10, 20)).Type);
        Assert.Equal(250, state.Economy.Coins);
    }

    [Fact]
    public void FarmingLoop_TillsPlantsWatersSleepsAndHarvests()
    {
        GameState state = CreateState();
        GridPosition farmTile = new(10, 20);

        Assert.True(state.Farming.Till(state.World, farmTile, state.Energy));
        Assert.Equal(TileType.Soil, state.World.GetTile(farmTile).Type);

        state.Inventory[8] = new InventorySlot { ItemId = "turnip_seed", Quantity = 1 };
        Assert.True(state.Farming.Plant(state.World, farmTile, state.Inventory, "turnip_seed", state.Content));
        Assert.NotNull(state.World.GetCrop(farmTile));

        Assert.True(state.Farming.Water(state.World, farmTile, state.Energy));
        state.Sleep();
        Assert.True(state.Farming.Water(state.World, farmTile, state.Energy));
        state.Sleep();

        Assert.True(state.Farming.Harvest(state.World, farmTile, state.Inventory, state.Content));
        Assert.Contains(state.Inventory.Slots, slot => slot.ItemId == "turnip" && slot.Quantity == 1);
    }

    [Fact]
    public void Inventory_StacksAndRemovesItems()
    {
        ContentDatabase content = LoadContent();
        Inventory inventory = new(2);

        Assert.True(inventory.Add("turnip_seed", 120, content.Items));
        Assert.Equal(99, inventory[0].Quantity);
        Assert.Equal(21, inventory[1].Quantity);

        Assert.True(inventory.Remove("turnip_seed", 100));
        Assert.Equal(20, inventory.Slots.Sum(slot => slot.Quantity));
    }

    private static GameState CreateState()
    {
        return new GameState(LoadContent());
    }

    private static ContentDatabase LoadContent()
    {
        string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        return ContentDatabase.Load(dataDirectory);
    }
}
