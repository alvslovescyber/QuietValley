using PixelHomestead.Core.Core;
using PixelHomestead.Core.Data;
using PixelHomestead.Core.Items;
using PixelHomestead.Core.Systems;
using PixelHomestead.Core.World;

string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
ContentDatabase content = ContentDatabase.Load(dataDirectory);
GameState state = new(content);

Assert(content.Items.ContainsKey("hoe"), "Hoe item definition should load.");
Assert(content.Crops.ContainsKey("turnip"), "Turnip crop definition should load.");
Assert(state.Inventory.Slots.Take(5).All(slot => !slot.IsEmpty), "Starter tools should fill first five hotbar slots.");

GridPosition farmTile = new(10, 20);
bool tilled = state.Farming.Till(state.World, farmTile, state.Energy);
Assert(tilled, "Hoe should till a starter farm tile.");
Assert(state.World.GetTile(farmTile).Type == TileType.Soil, "Farm tile should become soil.");

state.Inventory[8] = new InventorySlot { ItemId = "turnip_seed", Quantity = 1 };
bool planted = state.Farming.Plant(state.World, farmTile, state.Inventory, "turnip_seed", content);
Assert(planted, "Turnip seed should plant on tilled soil.");
Assert(state.World.GetCrop(farmTile) is not null, "Crop should exist after planting.");

state.Farming.Water(state.World, farmTile, state.Energy);
state.Sleep();
state.Farming.Water(state.World, farmTile, state.Energy);
state.Sleep();
bool harvested = state.Farming.Harvest(state.World, farmTile, state.Inventory, content);
Assert(harvested, "Mature crop should harvest after watered growth days.");

Console.WriteLine("Pixel Homestead smoke checks passed.");

static void Assert(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}
