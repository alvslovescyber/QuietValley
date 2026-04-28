using System.Text.Json;
using System.Text.Json.Serialization;
using PixelHomestead.Core.Core;
using PixelHomestead.Core.Data;
using PixelHomestead.Core.Energy;
using PixelHomestead.Core.Items;
using PixelHomestead.Core.Systems;
using PixelHomestead.Core.World;

namespace PixelHomestead.Core.Saving;

public sealed class SaveManager
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public string SavePath { get; }

    public SaveManager(string gameName = "PixelHomestead")
    {
        string applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string saveDirectory = Path.Combine(applicationData, gameName);
        Directory.CreateDirectory(saveDirectory);
        SavePath = Path.Combine(saveDirectory, "savegame.json");
    }

    public void Save(GameState state)
    {
        SaveData saveData = SaveData.FromState(state);
        string json = JsonSerializer.Serialize(saveData, SerializerOptions);
        File.WriteAllText(SavePath, json);
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
            state.Player.WorldX = PlayerWorldX;
            state.Player.WorldY = PlayerWorldY;
            state.Player.Facing = Facing;
            state.Player.SelectedHotbarIndex = Math.Clamp(SelectedHotbarIndex, 0, 8);
            state.Time.SetState(Day, Season, MinutesSinceMidnight);
            state.Energy.SetCurrent(Energy);
            state.Economy.SetCoins(Coins);
            state.Inventory.Clear();

            foreach (SaveInventorySlot slot in Inventory)
            {
                if (slot.Index >= 0 && slot.Index < state.Inventory.Capacity)
                {
                    state.Inventory[slot.Index] = new InventorySlot { ItemId = slot.ItemId, Quantity = slot.Quantity };
                }
            }

            state.Economy.RestoreShippingBin(
                ShippingBin.Select(slot => new InventorySlot { ItemId = slot.ItemId, Quantity = slot.Quantity })
            );

            foreach (SaveTile tile in TileOverrides)
            {
                state.World.SetTile(new GridPosition(tile.X, tile.Y), tile.Type);
            }

            foreach (SaveCrop crop in Crops)
            {
                state.World.SetCrop(
                    new GridPosition(crop.X, crop.Y),
                    new CropState
                    {
                        CropId = crop.CropId,
                        GrowthProgress = crop.GrowthProgress,
                        WateredToday = crop.WateredToday,
                    }
                );
            }

            return state;
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
