using PixelHomestead.Core.Core;
using PixelHomestead.Core.Data;
using PixelHomestead.Core.Items;
using PixelHomestead.Core.World;

namespace PixelHomestead.Core.Systems;

public sealed class FishingSystem
{
    private readonly Random _random = new();
    private readonly string[] _fishIds = ["small_fish", "pond_carp", "river_minnow", "golden_fish"];

    public string? TryCatchFish(GameWorld world, GridPosition playerPosition, Direction facing, Inventory inventory, ContentDatabase content, EnergySystem energy)
    {
        GridPosition target = playerPosition.Neighbor(facing);
        bool nearWater = world.GetTile(target).IsWater || AdjacentPositions(playerPosition).Any(position => world.GetTile(position).IsWater);
        if (!nearWater || !energy.Spend(3))
        {
            return null;
        }

        string fishId = _fishIds[_random.Next(_fishIds.Length)];
        inventory.Add(fishId, 1, content.Items);
        return fishId;
    }

    private static IEnumerable<GridPosition> AdjacentPositions(GridPosition position)
    {
        yield return position.Neighbor(Direction.Up);
        yield return position.Neighbor(Direction.Down);
        yield return position.Neighbor(Direction.Left);
        yield return position.Neighbor(Direction.Right);
    }
}
