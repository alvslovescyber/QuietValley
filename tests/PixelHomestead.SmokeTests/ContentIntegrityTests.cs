using PixelHomestead.Core.Data;
using PixelHomestead.Core.Items;
using Xunit;

namespace PixelHomestead.SmokeTests;

public sealed class ContentIntegrityTests
{
    [Fact]
    public void DataFiles_AllCrossReferencesResolve()
    {
        ContentDatabase content = LoadContent();

        foreach (ToolDefinition tool in content.Tools.Values)
        {
            Assert.True(
                content.Items.ContainsKey(tool.ItemId),
                $"Tool '{tool.Id}' references missing item '{tool.ItemId}'."
            );
            Assert.Equal(ItemType.Tool, content.Items[tool.ItemId].Type);
        }

        foreach (CropDefinition crop in content.Crops.Values)
        {
            Assert.True(
                content.Items.ContainsKey(crop.SeedItemId),
                $"Crop '{crop.Id}' references missing seed '{crop.SeedItemId}'."
            );
            Assert.True(
                content.Items.ContainsKey(crop.HarvestItemId),
                $"Crop '{crop.Id}' references missing harvest '{crop.HarvestItemId}'."
            );
            Assert.Equal(ItemType.Seed, content.Items[crop.SeedItemId].Type);
            Assert.Equal(ItemType.Crop, content.Items[crop.HarvestItemId].Type);
            Assert.True(crop.GrowthDays > 0);
        }

        foreach (var fish in content.Fish.Values)
        {
            Assert.True(
                content.Items.ContainsKey(fish.ItemId),
                $"Fish '{fish.Id}' references missing item '{fish.ItemId}'."
            );
            Assert.Equal(ItemType.Fish, content.Items[fish.ItemId].Type);
            Assert.True(fish.RarityWeight > 0);
        }
    }

    [Fact]
    public void DataFiles_ItemsHaveValidStackAndEconomyValues()
    {
        ContentDatabase content = LoadContent();

        foreach (ItemDefinition item in content.Items.Values)
        {
            Assert.False(string.IsNullOrWhiteSpace(item.Id));
            Assert.False(string.IsNullOrWhiteSpace(item.DisplayName));
            Assert.InRange(item.MaxStack, 1, 999);
            Assert.True(item.SellPrice >= 0);
            if (item.Type == ItemType.Tool)
            {
                Assert.Equal(1, item.MaxStack);
                Assert.Equal(0, item.SellPrice);
            }
        }
    }

    private static ContentDatabase LoadContent()
    {
        string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        return ContentDatabase.Load(dataDirectory);
    }
}
