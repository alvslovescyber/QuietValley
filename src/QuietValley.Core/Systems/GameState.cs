using QuietValley.Core.Data;
using QuietValley.Core.Economy;
using QuietValley.Core.Energy;
using QuietValley.Core.Farming;
using QuietValley.Core.Fishing;
using QuietValley.Core.Items;
using QuietValley.Core.Player;
using QuietValley.Core.Time;
using QuietValley.Core.World;

namespace QuietValley.Core.Systems;

public sealed class GameState
{
    public GameState(ContentDatabase content)
    {
        Content = content;
        World = GameWorld.CreateStarterWorld();
        Inventory = new Inventory(36);
        Player = new PlayerState();
        Time = new TimeSystem();
        Energy = new EnergySystem();
        Economy = new EconomySystem();
        Farming = new FarmingSystem();
        Fishing = new FishingSystem();

        SeedStartingInventory();
    }

    public ContentDatabase Content { get; }
    public GameWorld World { get; }
    public Inventory Inventory { get; }
    public PlayerState Player { get; }
    public TimeSystem Time { get; }
    public EnergySystem Energy { get; }
    public EconomySystem Economy { get; }
    public FarmingSystem Farming { get; }
    public FishingSystem Fishing { get; }
    public string StatusMessage { get; set; } = "Welcome to QuietValley";

    public void Sleep()
    {
        Farming.AdvanceDay(World, Content);
        Economy.SellShippedItems(Content);
        Energy.Restore();
        Time.AdvanceDay();
        StatusMessage = $"Day {Time.Day} begins. Energy restored.";
    }

    private void SeedStartingInventory()
    {
        Inventory.Add("hoe", 1, Content.Items);
        Inventory.Add("watering_can", 1, Content.Items);
        Inventory.Add("axe", 1, Content.Items);
        Inventory.Add("pickaxe", 1, Content.Items);
        Inventory.Add("fishing_rod", 1, Content.Items);
        Inventory.Add("turnip_seed", 8, Content.Items);
        Inventory.Add("carrot_seed", 6, Content.Items);
    }
}
