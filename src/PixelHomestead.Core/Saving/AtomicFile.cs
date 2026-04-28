namespace PixelHomestead.Core.Saving;

public static class AtomicFile
{
    public static void WriteAllText(string path, string contents)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        string fileName = Path.GetFileName(path);
        string temporaryDirectory = string.IsNullOrWhiteSpace(directory) ? "." : directory;
        string temporaryPath = Path.Combine(temporaryDirectory, $".{fileName}.{Guid.NewGuid():N}.tmp");

        try
        {
            File.WriteAllText(temporaryPath, contents);
            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, null);
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }
}
