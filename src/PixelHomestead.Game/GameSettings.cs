using System.Text.Json;
using PixelHomestead.Core.Saving;

namespace PixelHomestead.Game;

public sealed record GameSettings
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    public float MusicVolume { get; set; } = 0.6f;
    public float SfxVolume { get; set; } = 0.8f;
    public int WindowScale { get; set; } = 2;
    public bool Fullscreen { get; set; }
    public bool ShowCollisionDebug { get; set; }

    public static string SettingsPath()
    {
        string applicationData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        string saveDirectory = Path.Combine(applicationData, "PixelHomestead");
        Directory.CreateDirectory(saveDirectory);
        return Path.Combine(saveDirectory, "settings.json");
    }

    public static GameSettings Load()
    {
        string path = SettingsPath();
        if (!File.Exists(path))
        {
            return new GameSettings();
        }

        try
        {
            string json = File.ReadAllText(path);
            return (
                JsonSerializer.Deserialize<GameSettings>(json, SerializerOptions) ?? new GameSettings()
            ).Normalize();
        }
        catch (JsonException)
        {
            return new GameSettings();
        }
        catch (IOException)
        {
            return new GameSettings();
        }
    }

    public void Save()
    {
        Normalize();
        AtomicFile.WriteAllText(SettingsPath(), JsonSerializer.Serialize(this, SerializerOptions));
    }

    private GameSettings Normalize()
    {
        MusicVolume = Math.Clamp(MusicVolume, 0f, 1f);
        SfxVolume = Math.Clamp(SfxVolume, 0f, 1f);
        WindowScale = Math.Clamp(WindowScale, 1, 4);
        return this;
    }
}
