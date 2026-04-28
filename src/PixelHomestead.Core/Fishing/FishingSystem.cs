using PixelHomestead.Core.Core;
using PixelHomestead.Core.Data;
using PixelHomestead.Core.Energy;
using PixelHomestead.Core.Items;
using PixelHomestead.Core.World;

namespace PixelHomestead.Core.Fishing;

public sealed class FishingSystem
{
    private readonly Random _random = new();

    public string? TryCatchFish(
        GameWorld world,
        GridPosition playerPosition,
        Direction facing,
        Inventory inventory,
        ContentDatabase content,
        EnergySystem energy
    )
    {
        GridPosition target = playerPosition.Neighbor(facing);
        bool nearWater =
            world.GetTile(target).IsWater
            || AdjacentPositions(playerPosition).Any(position => world.GetTile(position).IsWater);
        if (!nearWater || !energy.HasEnough(3))
        {
            return null;
        }

        return CatchFish(inventory, content, energy);
    }

    public string? TryCatchFishAtWater(
        GameWorld world,
        GridPosition waterPosition,
        Inventory inventory,
        ContentDatabase content,
        EnergySystem energy
    )
    {
        if (!world.GetTile(waterPosition).IsWater || !energy.HasEnough(3))
        {
            return null;
        }

        return CatchFish(inventory, content, energy);
    }

    private string? CatchFish(Inventory inventory, ContentDatabase content, EnergySystem energy)
    {
        string fishId = SelectFish(content);
        if (!inventory.Add(fishId, 1, content.Items))
        {
            return null;
        }

        energy.Spend(3);
        return fishId;
    }

    private string SelectFish(ContentDatabase content)
    {
        int totalWeight = content.Fish.Values.Sum(fish => Math.Max(0, fish.RarityWeight));
        if (totalWeight <= 0)
        {
            return "small_fish";
        }

        int roll = _random.Next(totalWeight);
        foreach (FishDefinition fish in content.Fish.Values)
        {
            roll -= Math.Max(0, fish.RarityWeight);
            if (roll < 0)
            {
                return fish.ItemId;
            }
        }

        return content.Fish.Values.First().ItemId;
    }

    private static IEnumerable<GridPosition> AdjacentPositions(GridPosition position)
    {
        yield return position.Neighbor(Direction.Up);
        yield return position.Neighbor(Direction.Down);
        yield return position.Neighbor(Direction.Left);
        yield return position.Neighbor(Direction.Right);
    }
}
