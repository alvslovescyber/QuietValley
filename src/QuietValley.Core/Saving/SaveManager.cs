using System.Text.Json;
using System.Text.Json.Serialization;
using QuietValley.Core.Core;
using QuietValley.Core.Data;
using QuietValley.Core.Energy;
using QuietValley.Core.Items;
using QuietValley.Core.Systems;
using QuietValley.Core.World;

namespace QuietValley.Core.Saving;

public readonly record struct FarmSaveInfo(string FarmName, DateTime LastSaved);

public sealed class SaveManager
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public string FarmName { get; }
    public string SavePath { get; }

    public SaveManager(string farmName, string gameName = "QuietValley")
    {
        string applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string sanitized = SanitizeName(farmName);
        if (string.IsNullOrWhiteSpace(sanitized))
        {
            sanitized = "Farm";
        }

        FarmName = sanitized;
        string saveDirectory = Path.Combine(applicationData, gameName, "farms", sanitized);
        Directory.CreateDirectory(saveDirectory);
        SavePath = Path.Combine(saveDirectory, "savegame.json");
    }

    public static FarmSaveInfo[] ListFarms(string gameName = "QuietValley")
    {
        string applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string farmsDirectory = Path.Combine(applicationData, gameName, "farms");
        if (!Directory.Exists(farmsDirectory))
        {
            return [];
        }

        return Directory
            .GetDirectories(farmsDirectory)
            .Select(dir =>
            {
                string name = Path.GetFileName(dir);
                string savePath = Path.Combine(dir, "savegame.json");
                DateTime lastSaved = File.Exists(savePath)
                    ? File.GetLastWriteTime(savePath)
                    : Directory.GetCreationTime(dir);
                return new FarmSaveInfo(name, lastSaved);
            })
            .OrderByDescending(f => f.LastSaved)
            .ToArray();
    }

    private static string SanitizeName(string name)
    {
        char[] invalid = Path.GetInvalidFileNameChars();
        string result = new(name.Where(c => !invalid.Contains(c)).ToArray());
        result = result.Trim();
        return result.Length > 32 ? result[..32] : result;
    }

    public void Save(GameState state)
    {
        SaveData saveData = SaveData.FromState(state);
        string json = JsonSerializer.Serialize(saveData, SerializerOptions);
        AtomicFile.WriteAllText(SavePath, json);
    }

    public GameState Load(ContentDatabase content)
    {
        if (!File.Exists(SavePath))
        {
            return new GameState(content);
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            SaveData saveData = JsonSerializer.Deserialize<SaveData>(json, SerializerOptions) ?? new SaveData();
            return saveData.ToState(content);
        }
        catch (JsonException)
        {
            return new GameState(content);
        }
        catch (IOException)
        {
            return new GameState(content);
        }
    }

    private sealed record SaveData
    {
        public int PlayerX { get; init; } = 14;
        public int PlayerY { get; init; } = 18;
        public float PlayerWorldX { get; init; } = 14 * 16;
        public float PlayerWorldY { get; init; } = 18 * 16;
        public Direction Facing { get; init; } = Direction.Down;
        public int SelectedHotbarIndex { get; init; }
        public int Day { get; init; } = 1;
        public string Season { get; init; } = "Spring";
        public int MinutesSinceMidnight { get; init; } = 360;
        public int Energy { get; init; } = EnergySystem.MaximumEnergy;
        public int Coins { get; init; } = 250;
        public List<SaveInventorySlot> Inventory { get; init; } = [];
        public List<SaveInventorySlot> ShippingBin { get; init; } = [];
        public List<SaveTile> TileOverrides { get; init; } = [];
        public List<SaveCrop> Crops { get; init; } = [];

        public static SaveData FromState(GameState state)
        {
            return new SaveData
            {
                PlayerX = state.Player.TilePosition.X,
                PlayerY = state.Player.TilePosition.Y,
                PlayerWorldX = state.Player.WorldX,
                PlayerWorldY = state.Player.WorldY,
                Facing = state.Player.Facing,
                SelectedHotbarIndex = state.Player.SelectedHotbarIndex,
                Day = state.Time.Day,
                Season = state.Time.Season,
                MinutesSinceMidnight = state.Time.MinutesSinceMidnight,
                Energy = state.Energy.CurrentEnergy,
                Coins = state.Economy.Coins,
                Inventory = state
                    .Inventory.Slots.Select(
                        (slot, index) =>
                            new SaveInventorySlot
                            {
                                Index = index,
                                ItemId = slot.ItemId,
                                Quantity = slot.Quantity,
                            }
                    )
                    .Where(slot => !string.IsNullOrWhiteSpace(slot.ItemId) && slot.Quantity > 0)
                    .ToList(),
                ShippingBin = state
                    .Economy.ShippingBin.Select(
                        (slot, index) =>
                            new SaveInventorySlot
                            {
                                Index = index,
                                ItemId = slot.ItemId,
                                Quantity = slot.Quantity,
                            }
                    )
                    .Where(slot => !string.IsNullOrWhiteSpace(slot.ItemId) && slot.Quantity > 0)
                    .ToList(),
                TileOverrides = state
                    .World.Tiles()
                    .Select(entry => new SaveTile
                    {
                        X = entry.Position.X,
                        Y = entry.Position.Y,
                        Type = entry.Tile.Type,
                    })
                    .ToList(),
                Crops = state
                    .World.Crops.Select(entry => new SaveCrop
                    {
                        X = entry.Key.X,
                        Y = entry.Key.Y,
                        CropId = entry.Value.CropId,
                        GrowthProgress = entry.Value.GrowthProgress,
                        WateredToday = entry.Value.WateredToday,
                    })
                    .ToList(),
            };
        }

        public GameState ToState(ContentDatabase content)
        {
            GameState state = new(content);
            state.Player.TilePosition = new GridPosition(PlayerX, PlayerY);
            state.Player.WorldX = NormalizeWorldCoordinate(PlayerWorldX, PlayerX);
            state.Player.WorldY = NormalizeWorldCoordinate(PlayerWorldY, PlayerY);
            state.Player.Facing = Enum.IsDefined(Facing) ? Facing : Direction.Down;
            state.Player.SelectedHotbarIndex = Math.Clamp(SelectedHotbarIndex, 0, 8);
            state.Time.SetState(Day, Season, MinutesSinceMidnight);
            state.Energy.SetCurrent(Energy);
            state.Economy.SetCoins(Coins);
            state.Inventory.Clear();

            foreach (SaveInventorySlot slot in Inventory)
            {
                InventorySlot? restoredSlot = RestoreInventorySlot(slot, content);
                if (restoredSlot is not null && slot.Index >= 0 && slot.Index < state.Inventory.Capacity)
                {
                    state.Inventory[slot.Index] = restoredSlot;
                }
            }

            state.Economy.RestoreShippingBin(
                ShippingBin.Select(slot => RestoreShippingSlot(slot, content)).OfType<InventorySlot>()
            );

            foreach (SaveTile tile in TileOverrides)
            {
                if (Enum.IsDefined(tile.Type))
                {
                    state.World.SetTile(new GridPosition(tile.X, tile.Y), tile.Type);
                }
            }

            foreach (SaveCrop crop in Crops)
            {
                if (!content.Crops.TryGetValue(crop.CropId, out CropDefinition? cropDefinition))
                {
                    continue;
                }

                state.World.SetCrop(
                    new GridPosition(crop.X, crop.Y),
                    new CropState
                    {
                        CropId = crop.CropId,
                        GrowthProgress = Math.Clamp(crop.GrowthProgress, 0, cropDefinition.GrowthDays),
                        WateredToday = crop.WateredToday,
                    }
                );
            }

            if (state.World.BlocksMovement(state.Player.TilePosition))
            {
                state.Player.TilePosition = new GridPosition(14, 18);
                state.Player.WorldX = 14 * 16;
                state.Player.WorldY = 18 * 16;
                state.Player.Facing = Direction.Down;
            }

            return state;
        }

        private static float NormalizeWorldCoordinate(float coordinate, int tileCoordinate)
        {
            return float.IsFinite(coordinate) ? coordinate : tileCoordinate * 16;
        }

        private static InventorySlot? RestoreInventorySlot(SaveInventorySlot slot, ContentDatabase content)
        {
            if (slot.ItemId is null || !content.Items.TryGetValue(slot.ItemId, out ItemDefinition? item))
            {
                return null;
            }

            int quantity = Math.Clamp(slot.Quantity, 1, item.MaxStack);
            return new InventorySlot { ItemId = slot.ItemId, Quantity = quantity };
        }

        private static InventorySlot? RestoreShippingSlot(SaveInventorySlot slot, ContentDatabase content)
        {
            if (
                slot.ItemId is null
                || !content.Items.TryGetValue(slot.ItemId, out ItemDefinition? item)
                || item.Type == ItemType.Tool
                || item.SellPrice <= 0
            )
            {
                return null;
            }

            int quantity = Math.Clamp(slot.Quantity, 1, item.MaxStack);
            return new InventorySlot { ItemId = slot.ItemId, Quantity = quantity };
        }
    }

    private sealed record SaveInventorySlot
    {
        public int Index { get; init; }
        public string? ItemId { get; init; }
        public int Quantity { get; init; }
    }

    private sealed record SaveTile
    {
        public int X { get; init; }
        public int Y { get; init; }
        public TileType Type { get; init; }
    }

    private sealed record SaveCrop
    {
        public int X { get; init; }
        public int Y { get; init; }
        public required string CropId { get; init; }
        public int GrowthProgress { get; init; }
        public bool WateredToday { get; init; }
    }
}
