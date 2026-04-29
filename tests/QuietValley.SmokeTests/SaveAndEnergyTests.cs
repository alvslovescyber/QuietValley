using QuietValley.Core.Core;
using QuietValley.Core.Data;
using QuietValley.Core.Energy;
using QuietValley.Core.Items;
using QuietValley.Core.Saving;
using QuietValley.Core.Systems;
using QuietValley.Core.World;
using Xunit;

namespace QuietValley.SmokeTests;

public sealed class SaveAndEnergyTests
{
    [Fact]
    public void SaveManager_RoundTripsPlayerPositionHotbarAndEnergy()
    {
        ContentDatabase content = LoadContent();
        GameState state = new(content);
        string testGameName = $"QuietValleyTests-{Guid.NewGuid():N}";
        SaveManager manager = new(testGameName);

        try
        {
            state.Player.TilePosition = new GridPosition(16, 20);
            state.Player.WorldX = 123.5f;
            state.Player.WorldY = 246.25f;
            state.Player.SelectedHotbarIndex = 4;
            Assert.True(state.Energy.Spend(17));

            manager.Save(state);

            GameState loaded = manager.Load(content);

            Assert.Equal(new GridPosition(16, 20), loaded.Player.TilePosition);
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
        string testGameName = $"QuietValleyTests-{Guid.NewGuid():N}";
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
    public void SaveManager_RespawnsPlayerIfSavedInsideBlockedHouseTile()
    {
        ContentDatabase content = LoadContent();
        GameState state = new(content);
        string testGameName = $"QuietValleyTests-{Guid.NewGuid():N}";
        SaveManager manager = new(testGameName);

        try
        {
            state.Player.TilePosition = new GridPosition(7, 8);
            state.Player.WorldX = 7 * 16;
            state.Player.WorldY = 8 * 16;

            manager.Save(state);

            GameState loaded = manager.Load(content);

            Assert.Equal(new GridPosition(14, 18), loaded.Player.TilePosition);
            Assert.Equal(14 * 16, loaded.Player.WorldX);
            Assert.Equal(18 * 16, loaded.Player.WorldY);
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
    public void SaveManager_CorruptSaveFallsBackToNewGame()
    {
        ContentDatabase content = LoadContent();
        string testGameName = $"QuietValleyTests-{Guid.NewGuid():N}";
        SaveManager manager = new(testGameName);

        try
        {
            File.WriteAllText(manager.SavePath, "{ definitely not valid json");

            GameState loaded = manager.Load(content);

            Assert.Equal(new GridPosition(14, 18), loaded.Player.TilePosition);
            Assert.Equal(250, loaded.Economy.Coins);
            Assert.Equal(EnergySystem.MaximumEnergy, loaded.Energy.CurrentEnergy);
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
    public void AtomicFile_ReplacesExistingFileAndCleansTemporaryFiles()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"QuietValleyAtomicTests-{Guid.NewGuid():N}");
        string path = Path.Combine(directory, "settings.json");

        try
        {
            AtomicFile.WriteAllText(path, "first");
            AtomicFile.WriteAllText(path, "second");

            Assert.Equal("second", File.ReadAllText(path));
            Assert.Empty(Directory.EnumerateFiles(directory, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void SaveManager_SanitizesInvalidSaveEntries()
    {
        ContentDatabase content = LoadContent();
        string testGameName = $"QuietValleyTests-{Guid.NewGuid():N}";
        SaveManager manager = new(testGameName);

        try
        {
            File.WriteAllText(
                manager.SavePath,
                """
                {
                  "PlayerX": 14,
                  "PlayerY": 18,
                  "SelectedHotbarIndex": 99,
                  "Energy": 500,
                  "Coins": -25,
                  "Inventory": [
                    { "Index": 0, "ItemId": "unknown_item", "Quantity": 5 },
                    { "Index": 1, "ItemId": "turnip_seed", "Quantity": 500 }
                  ],
                  "ShippingBin": [
                    { "Index": 0, "ItemId": "hoe", "Quantity": 1 },
                    { "Index": 1, "ItemId": "turnip", "Quantity": 500 }
                  ],
                  "TileOverrides": [
                    { "X": 11, "Y": 20, "Type": "Soil" }
                  ],
                  "Crops": [
                    { "X": 10, "Y": 20, "CropId": "missing_crop", "GrowthProgress": 99, "WateredToday": true },
                    { "X": 11, "Y": 20, "CropId": "turnip", "GrowthProgress": 99, "WateredToday": true }
                  ]
                }
                """
            );

            GameState loaded = manager.Load(content);

            Assert.Equal(8, loaded.Player.SelectedHotbarIndex);
            Assert.Equal(EnergySystem.MaximumEnergy, loaded.Energy.CurrentEnergy);
            Assert.Equal(0, loaded.Economy.Coins);
            Assert.True(loaded.Inventory[0].IsEmpty);
            Assert.Equal("turnip_seed", loaded.Inventory[1].ItemId);
            Assert.Equal(99, loaded.Inventory[1].Quantity);
            Assert.Single(loaded.Economy.ShippingBin);
            Assert.Equal("turnip", loaded.Economy.ShippingBin[0].ItemId);
            Assert.Equal(99, loaded.Economy.ShippingBin[0].Quantity);
            Assert.Null(loaded.World.GetCrop(new GridPosition(10, 20)));
            Assert.Equal(2, loaded.World.GetCrop(new GridPosition(11, 20))?.GrowthProgress);
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
