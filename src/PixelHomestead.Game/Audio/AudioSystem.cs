namespace PixelHomestead.Game.Audio;

public sealed class AudioSystem
{
    public float MusicVolume { get; private set; } = 0.6f;
    public float SfxVolume { get; private set; } = 0.8f;

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
    }

    public void PlayMusic(string trackName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackName);
    }
}
