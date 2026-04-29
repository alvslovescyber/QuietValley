using Xunit;

namespace QuietValley.SmokeTests;

public sealed class ReleaseReadinessTests
{
    [Fact]
    public void RepositoryTextFiles_DoNotContainLegacyProjectBranding()
    {
        string root = FindRepositoryRoot();
        string[] legacyNames =
        [
            string.Concat("Pixel", "Homestead"),
            string.Concat("Pixel", " ", "Homestead"),
            string.Concat("pixel", "-", "homestead"),
        ];

        foreach (string file in EnumerateTextFiles(root))
        {
            string text = File.ReadAllText(file);
            foreach (string legacyName in legacyNames)
            {
                Assert.False(
                    text.Contains(legacyName, StringComparison.OrdinalIgnoreCase),
                    $"{RelativePath(root, file)} still contains legacy branding '{legacyName}'."
                );
            }
        }
    }

    [Fact]
    public void ReleasePackaging_UsesQuietValleyInstallableArtifactNames()
    {
        string root = FindRepositoryRoot();
        string packageScript = File.ReadAllText(Path.Combine(root, "scripts", "package_release.sh"));
        string releaseWorkflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "release.yml"));
        string readme = File.ReadAllText(Path.Combine(root, "README.md"));

        Assert.Contains("MAC_APP_NAME=\"QuietValley\"", packageScript);
        Assert.Contains("MAC_EXECUTABLE_NAME=\"QuietValley.Game\"", packageScript);
        Assert.Contains("QuietValley-$runtime.zip", packageScript);
        Assert.Contains("QuietValley-win-x64.zip", packageScript);
        Assert.Contains("Info.plist", packageScript);
        Assert.Contains("mktemp -d", packageScript);
        Assert.Contains("notarize_dmg_if_configured", packageScript);
        Assert.Contains("QuietValley.app", releaseWorkflow);
        Assert.Contains("QuietValley.app", readme);
        Assert.Contains("QuietValley-win-x64.zip", readme);
        string spacedAppName = string.Concat("Quiet", " ", "Valley.app");
        Assert.DoesNotContain(spacedAppName, packageScript);
        Assert.DoesNotContain(spacedAppName, releaseWorkflow);
        Assert.DoesNotContain(spacedAppName, readme);
    }

    [Fact]
    public void FormattingEntrypoints_RespectCSharpierIgnore()
    {
        string root = FindRepositoryRoot();
        string csharpierIgnore = File.ReadAllText(Path.Combine(root, ".csharpierignore"));
        string[] expectedIgnoredPaths = ["artifacts/", "bin/", "obj/", "TestResults/", "coverage/"];
        string[] formattingEntrypoints =
        [
            Path.Combine(root, "scripts", "check.sh"),
            Path.Combine(root, "scripts", "format.sh"),
            Path.Combine(root, "scripts", "package_release.sh"),
            Path.Combine(root, ".github", "workflows", "ci.yml"),
            Path.Combine(root, ".github", "pull_request_template.md"),
        ];

        foreach (string ignoredPath in expectedIgnoredPaths)
        {
            Assert.Contains(ignoredPath, csharpierIgnore);
        }

        foreach (string entrypoint in formattingEntrypoints)
        {
            string text = File.ReadAllText(entrypoint);
            Assert.Contains("--ignore-path .csharpierignore", text);
        }
    }

    private static IEnumerable<string> EnumerateTextFiles(string root)
    {
        Stack<string> directories = new([root]);
        while (directories.Count > 0)
        {
            string directory = directories.Pop();

            foreach (string childDirectory in Directory.EnumerateDirectories(directory))
            {
                string name = Path.GetFileName(childDirectory);
                if (!IgnoredDirectories.Contains(name))
                {
                    directories.Push(childDirectory);
                }
            }

            foreach (string file in Directory.EnumerateFiles(directory))
            {
                if (!IgnoredExtensions.Contains(Path.GetExtension(file)))
                {
                    yield return file;
                }
            }
        }
    }

    private static string FindRepositoryRoot()
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

        throw new DirectoryNotFoundException("Could not find QuietValley.sln.");
    }

    private static string RelativePath(string root, string file)
    {
        return Path.GetRelativePath(root, file);
    }

    private static readonly HashSet<string> IgnoredDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git",
        ".vs",
        "artifacts",
        "bin",
        "coverage",
        "obj",
        "TestResults",
    };

    private static readonly HashSet<string> IgnoredExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".dll",
        ".dmg",
        ".exe",
        ".ico",
        ".jpg",
        ".jpeg",
        ".pdb",
        ".png",
        ".trx",
        ".zip",
    };
}
