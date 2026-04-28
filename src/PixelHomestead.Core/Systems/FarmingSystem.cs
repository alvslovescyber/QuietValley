using PixelHomestead.Core.Core;
using PixelHomestead.Core.Data;
using PixelHomestead.Core.Items;
using PixelHomestead.Core.World;

namespace PixelHomestead.Core.Systems;

public sealed class FarmingSystem
{
    public bool Till(GameWorld world, GridPosition target, EnergySystem energy)
    {
        Tile tile = world.GetTile(target);
        if (!tile.CanTill || !energy.Spend(2))
        {
            return false;
        }

        world.SetTile(target, TileType.Soil);
        return true;
    }

    public bool Water(GameWorld world, GridPosition target, EnergySystem energy)
    {
        CropState? crop = world.GetCrop(target);
        if (world.GetTile(target).Type != TileType.Soil || crop is null || !energy.Spend(1))
        {
            return false;
        }

        world.SetCrop(target, crop with { WateredToday = true });
        return true;
    }

    public bool Plant(GameWorld world, GridPosition target, Inventory inventory, string seedItemId, ContentDatabase content)
    {
        if (world.GetTile(target).Type != TileType.Soil || world.HasCrop(target))
        {
            return false;
        }

        CropDefinition? crop = content.FindCropBySeed(seedItemId);
        if (crop is null || !inventory.Remove(seedItemId, 1))
        {
            return false;
        }

        world.SetCrop(target, new CropState { CropId = crop.Id, GrowthProgress = 0, WateredToday = false });
        return true;
    }

    public bool Harvest(GameWorld world, GridPosition target, Inventory inventory, ContentDatabase content)
    {
        CropState? cropState = world.GetCrop(target);
        if (cropState is null || !content.Crops.TryGetValue(cropState.CropId, out CropDefinition? crop))
        {
            return false;
        }

        if (cropState.GrowthProgress < crop.GrowthDays)
        {
            return false;
        }

        if (!inventory.Add(crop.HarvestItemId, 1, content.Items))
        {
            return false;
        }

        world.RemoveCrop(target);
        return true;
    }

    public void AdvanceDay(GameWorld world, ContentDatabase content)
    {
        foreach ((GridPosition position, CropState cropState) in world.Crops.ToArray())
        {
            if (!content.Crops.TryGetValue(cropState.CropId, out CropDefinition? crop))
            {
                continue;
            }

            int growthProgress = crop.RequiresWater && !cropState.WateredToday
                ? cropState.GrowthProgress
                : Math.Min(crop.GrowthDays, cropState.GrowthProgress + 1);

            world.SetCrop(position, cropState with { GrowthProgress = growthProgress, WateredToday = false });
        }
    }
}
