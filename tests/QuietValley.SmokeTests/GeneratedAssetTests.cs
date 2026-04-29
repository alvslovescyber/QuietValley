using Xunit;

namespace QuietValley.SmokeTests;

public sealed class GeneratedAssetTests
{
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    [Fact]
    public void GeneratedAssets_UseCleanRuntimeToolSource()
    {
        string root = RepositoryRoot();
        string generated = Path.Combine(root, "src", "QuietValley.Game", "Assets", "Generated");

        AssertPng(Path.Combine(generated, "icons.png"));
        AssertPng(Path.Combine(generated, "tool_icons_runtime_source.png"));
        Assert.False(
            File.Exists(Path.Combine(generated, "tool_icons_ai_source.png")),
            "Legacy magenta-backed tool icon source should not be restored."
        );
    }

    [Fact]
    public void Readme_ShowcaseImagesExistAndArePngs()
    {
        string root = RepositoryRoot();
        string readme = File.ReadAllText(Path.Combine(root, "README.md"));
        string[] showcaseImages =
        [
            "docs/images/home-screen-showcase.png",
            "docs/images/runtime-assets-showcase.png",
            "docs/images/interior-town-assets.png",
            "docs/images/tool-and-item-icons.png",
        ];

        foreach (string image in showcaseImages)
        {
            Assert.Contains(image, readme);
            AssertPng(Path.Combine(root, image));
        }
    }

    private static void AssertPng(string path)
    {
        Assert.True(File.Exists(path), $"Missing generated PNG: {path}");
        using FileStream stream = File.OpenRead(path);
        Span<byte> signature = stackalloc byte[PngSignature.Length];
        Assert.Equal(PngSignature.Length, stream.Read(signature));
        Assert.True(signature.SequenceEqual(PngSignature), $"File is not a valid PNG: {path}");
    }

    private static string RepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "QuietValley.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
    }
}
