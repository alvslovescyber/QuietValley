using PixelHomestead.Core.Core;
using PixelHomestead.Core.Data;
using PixelHomestead.Core.Energy;
using PixelHomestead.Core.Items;
using PixelHomestead.Core.Saving;
using PixelHomestead.Core.Systems;
using PixelHomestead.Core.World;
using Xunit;

namespace PixelHomestead.SmokeTests;

public sealed class SaveAndEnergyTests
{
    [Fact]
    public void SaveManager_RoundTripsPlayerPositionHotbarAndEnergy()
    {
        ContentDatabase content = LoadContent();
        GameState state = new(content);
        string testGameName = $"PixelHomesteadTests-{Guid.NewGuid():N}";
        SaveManager manager = new(testGameName);

        try
        {
            state.Player.TilePosition = new GridPosition(7, 9);
            state.Player.WorldX = 123.5f;
            state.Player.WorldY = 246.25f;
            state.Player.SelectedHotbarIndex = 4;
            Assert.True(state.Energy.Spend(17));

            manager.Save(state);

            GameState loaded = manager.Load(content);

            Assert.Equal(new GridPosition(7, 9), loaded.Player.TilePosition);
            Assert.Equal(123.5f, loaded.Player.WorldX);
            Assert.Equal(246.25f, loaded.Player.WorldY);
            Assert.Equal(4, loaded.Player.SelectedHotbarIndex);
            Assert.Equal(EnergySystem.MaximumEnergy - 17, loaded.Energy.CurrentEnergy);
        }
        finally
        {
            string? saveDirectory = Path.GetDirectoryName(manager.SavePath);
            if (!string.IsNullOrWhiteSpace(saveDirectory) && Directory.Exists(saveDirectory))
            {
                Directory.Delete(saveDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void SaveManager_PersistsClearedWorldTilesAndShippingBin()
    {
        ContentDatabase content = LoadContent();
        GameState state = new(content);
        string testGameName = $"PixelHomesteadTests-{Guid.NewGuid():N}";
        SaveManager manager = new(testGameName);
        GridPosition clearedBush = new(25, 8);

        try
        {
            state.World.SetTile(clearedBush, TileType.Grass);
            state.Inventory[8] = new InventorySlot { ItemId = "turnip", Quantity = 2 };
            Assert.True(state.Economy.ShipFromInventory(state.Inventory, 8, content));

            manager.Save(state);

            GameState loaded = manager.Load(content);

            Assert.Equal(TileType.Grass, loaded.World.GetTile(clearedBush).Type);
            Assert.Single(loaded.Economy.ShippingBin);
            Assert.Equal("turnip", loaded.Economy.ShippingBin[0].ItemId);
            Assert.Equal(2, loaded.Economy.ShippingBin[0].Quantity);
        }
        finally
        {
            string? saveDirectory = Path.GetDirectoryName(manager.SavePath);
            if (!string.IsNullOrWhiteSpace(saveDirectory) && Directory.Exists(saveDirectory))
            {
                Directory.Delete(saveDirectory, recursive: true);
            }
        }
    }

    [Fact]
    public void Economy_DoesNotShipUnsellableTools()
    {
        ContentDatabase content = LoadContent();
        GameState state = new(content);

        Assert.False(state.Economy.ShipFromInventory(state.Inventory, 0, content));
        Assert.Empty(state.Economy.ShippingBin);
        Assert.Equal("hoe", state.Inventory[0].ItemId);
    }

    [Fact]
    public void Energy_HasEnoughDoesNotSpend()
    {
        EnergySystem energy = new();

        Assert.True(energy.HasEnough(3));
        Assert.Equal(EnergySystem.MaximumEnergy, energy.CurrentEnergy);
        Assert.True(energy.Spend(3));
        Assert.Equal(EnergySystem.MaximumEnergy - 3, energy.CurrentEnergy);
        Assert.False(energy.HasEnough(EnergySystem.MaximumEnergy));
    }

    private static ContentDatabase LoadContent()
    {
        string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        return ContentDatabase.Load(dataDirectory);
    }
}
