using Microsoft.Xna.Framework.Audio;

namespace QuietValley.Game.Audio;

public sealed class AudioSystem : IDisposable
{
    private readonly Dictionary<string, SoundEffect> _sfx = new();
    private SoundEffect? _musicEffect;
    private SoundEffectInstance? _musicInstance;

    public float MusicVolume { get; private set; } = 0.6f;
    public float SfxVolume { get; private set; } = 0.8f;
    public string? LastCue { get; private set; }

    public void Initialize()
    {
        Register("ui_select", SoundSynthesizer.UiSelect());
        Register("menu_open", SoundSynthesizer.MenuOpen());
        Register("menu_close", SoundSynthesizer.MenuClose());
        Register("inventory_open", SoundSynthesizer.InventoryOpen());
        Register("inventory_close", SoundSynthesizer.InventoryClose());
        Register("tool_use", SoundSynthesizer.ToolUse());
        Register("tool_fail", SoundSynthesizer.ToolFail());
        Register("item_pickup", SoundSynthesizer.ItemPickup());
        Register("fish_bite", SoundSynthesizer.FishBite());
        Register("fish_cast", SoundSynthesizer.FishCast());
        Register("fish_catch", SoundSynthesizer.FishCatch());
        Register("fish_escape", SoundSynthesizer.FishEscape());
        Register("sleep", SoundSynthesizer.Sleep());
        Register("save", SoundSynthesizer.Save());
        Register("door_open", SoundSynthesizer.DoorOpen());
        Register("door_close", SoundSynthesizer.DoorClose());
        Register("ship_item", SoundSynthesizer.ShipItem());
        Register("tool_hoe", SoundSynthesizer.ToolHoe());
        Register("tool_water", SoundSynthesizer.ToolWater());
        Register("tool_chop", SoundSynthesizer.ToolChop());
        Register("tool_hit", SoundSynthesizer.ToolHit());
        Register("tool_scythe", SoundSynthesizer.ToolScythe());
        Register("tool_hammer", SoundSynthesizer.ToolHammer());
        Register("tool_shovel", SoundSynthesizer.ToolShovel());
        Register("ui_pickup", SoundSynthesizer.UiPickup());
        Register("ui_drop", SoundSynthesizer.UiDrop());

        _musicEffect = new SoundEffect(
            SoundSynthesizer.AmbientMusic(),
            SoundSynthesizer.SampleRate,
            AudioChannels.Mono
        );
        _musicInstance = _musicEffect.CreateInstance();
        _musicInstance.IsLooped = true;
        _musicInstance.Volume = MusicVolume;
        _musicInstance.Play();
    }

    public void SetMusicVolume(float volume)
    {
        MusicVolume = Math.Clamp(volume, 0f, 1f);
        if (_musicInstance is not null)
        {
            _musicInstance.Volume = MusicVolume;
        }
    }

    public void SetSfxVolume(float volume)
    {
        SfxVolume = Math.Clamp(volume, 0f, 1f);
    }

    public void PlaySfx(string cueName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cueName);
        LastCue = cueName;
        if (_sfx.TryGetValue(cueName, out SoundEffect? sfx))
        {
            sfx.Play(SfxVolume, 0f, 0f);
        }
    }

    public void PlayMusic(string trackName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(trackName);
        LastCue = $"music:{trackName}";
        _musicInstance?.Play();
    }

    public void Dispose()
    {
        _musicInstance?.Dispose();
        _musicEffect?.Dispose();
        foreach (SoundEffect sfx in _sfx.Values)
        {
            sfx.Dispose();
        }
        _sfx.Clear();
    }

    private void Register(string name, byte[] pcm)
    {
        _sfx[name] = new SoundEffect(pcm, SoundSynthesizer.SampleRate, AudioChannels.Mono);
    }
}
