namespace PixelHomestead.Game.Audio;

public sealed class AudioSystem
{
    private readonly Queue<string> _recentCues = new();

    public float MusicVolume { get; private set; } = 0.6f;
    public float SfxVolume { get; private set; } = 0.8f;
    public string? LastCue { get; private set; }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Math.Clamp(volume, 0f, 1f);
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Math.Clamp(volume, 0f, 1f);
    }

    public void PlaySfx(string cueName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cueName);
        LastCue = cueName;
        _recentCues.Enqueue(cueName);
        while (_recentCues.Count > 12)
        {
            _recentCues.Dequeue();
        }
    }

    public void PlayMusic(string trackName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackName);
        LastCue = $"music:{trackName}";
    }

    public IReadOnlyCollection<string> RecentCues()
    {
        return _recentCues.ToArray();
    }
}
