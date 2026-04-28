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
        Assert.Equal(TileType.Water, state.World.GetTile(new GridPosition(38, 17)).Type);
        Assert.Equal(TileType.Water, state.World.GetTile(new GridPosition(28, 24)).Type);
        Assert.False(state.World.BlocksMovement(new GridPosition(28, 24)));
        Assert.True(state.World.BlocksMovement(new GridPosition(4, 5)));
        Assert.True(state.World.BlocksMovement(new GridPosition(11, 12)));
        Assert.True(state.World.BlocksMovement(new GridPosition(36, 28)));
        Assert.True(state.World.BlocksMovement(new GridPosition(33, 34)));
        Assert.False(state.World.BlocksMovement(new GridPosition(14, 18)));
        Assert.Equal(250, state.Economy.Coins);
    }

    [Fact]
    public void StarterWorld_ProceduralTilesExistBeyondAuthoredMap()
    {
        GameState state = CreateState();

        Assert.False(state.World.BlocksMovement(new GridPosition(-3, 10)));
        Assert.Contains(
            Enumerable.Range(60, 80),
            x => state.World.GetTile(new GridPosition(x, 14)).Type is TileType.Water or TileType.Tree or TileType.Bush
        );
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
    public void Farming_InvalidActionsDoNotSpendEnergyOrConsumeSeeds()
    {
        GameState state = CreateState();
        GridPosition waterTile = new(28, 24);
        GridPosition dirtTile = new(11, 20);
        int startingEnergy = state.Energy.CurrentEnergy;
        state.Inventory[8] = new InventorySlot { ItemId = "turnip_seed", Quantity = 1 };

        Assert.False(state.Farming.Till(state.World, waterTile, state.Energy));
        Assert.Equal(startingEnergy, state.Energy.CurrentEnergy);
        Assert.False(state.Farming.Plant(state.World, dirtTile, state.Inventory, "turnip_seed", state.Content));
        Assert.Equal(1, state.Inventory[8].Quantity);
        Assert.False(state.Farming.Water(state.World, dirtTile, state.Energy));
        Assert.Equal(startingEnergy, state.Energy.CurrentEnergy);
    }

    [Fact]
    public void Farming_WaterRequiredCropsOnlyGrowAfterWateredSleep()
    {
        GameState state = CreateState();
        GridPosition farmTile = new(12, 20);
        state.Inventory[8] = new InventorySlot { ItemId = "turnip_seed", Quantity = 1 };

        Assert.True(state.Farming.Till(state.World, farmTile, state.Energy));
        Assert.True(state.Farming.Plant(state.World, farmTile, state.Inventory, "turnip_seed", state.Content));

        state.Sleep();
        Assert.Equal(0, state.World.GetCrop(farmTile)?.GrowthProgress);

        Assert.True(state.Farming.Water(state.World, farmTile, state.Energy));
        state.Sleep();
        Assert.Equal(1, state.World.GetCrop(farmTile)?.GrowthProgress);
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

    [Fact]
    public void Inventory_MoveMergesStacksAndFailedAddDoesNotPartiallyMutate()
    {
        ContentDatabase content = LoadContent();
        Inventory inventory = new(2);

        Assert.False(inventory.Add("turnip_seed", 200, content.Items));
        Assert.All(inventory.Slots, slot => Assert.True(slot.IsEmpty));

        inventory[0] = new InventorySlot { ItemId = "turnip_seed", Quantity = 95 };
        inventory[1] = new InventorySlot { ItemId = "turnip_seed", Quantity = 4 };

        inventory.MoveOrMerge(1, 0, content.Items);

        Assert.Equal(99, inventory[0].Quantity);
        Assert.True(inventory[1].IsEmpty);
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
