using PixelHomestead.Core.Data;
using Xunit;

namespace PixelHomestead.SmokeTests;

public sealed class ContentDatabaseTests
{
    [Fact]
    public void Load_ReadsItemAndCropDefinitions()
    {
        ContentDatabase content = LoadContent();

        Assert.Contains("hoe", content.Items.Keys);
        Assert.Contains("turnip", content.Crops.Keys);
        Assert.Equal("Turnip Seed", content.Items["turnip_seed"].DisplayName);
        Assert.Equal("turnip", content.FindCropBySeed("turnip_seed")?.Id);
    }

    private static ContentDatabase LoadContent()
    {
        string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
        return ContentDatabase.Load(dataDirectory);
    }
}
