using Catalyst.Core;
using Godot;

namespace Catalyst.App;

public partial class SettingsService : Node
{
    public event Action? SettingsChanged;
    public AppSettings Current { get; private set; } = new();

    private string SettingsPath =>
        ProjectSettings.GlobalizePath("user://settings.json");

    public override void _Ready()
    {
        Current = AtomicJsonStore.LoadOrDefault(SettingsPath, static () => new AppSettings());
        Current.SchemaVersion = 2;
        ApplyRuntimeSettings();
        CatalystLog.Info("Core", "Settings service ready.");
    }

    public void Save()
    {
        AtomicJsonStore.Save(SettingsPath, Current);
    }

    public void ApplyAndSave()
    {
        ApplyRuntimeSettings();
        Save();
        SettingsChanged?.Invoke();
    }

    public void ApplyRuntimeSettings()
    {
        Engine.MaxFps = Math.Max(0, Current.FrameRateLimit);
        ApplyBusVolume("Master", Current.MasterVolume);
        EnsureAudioBus("Music");
        EnsureAudioBus("SFX");
        ApplyBusVolume("Music", Current.MusicVolume);
        ApplyBusVolume("SFX", Current.EffectsVolume);

        if (!DisplayServer.GetName().Equals("headless", StringComparison.OrdinalIgnoreCase))
        {
            DisplayServer.WindowSetMode(Current.Fullscreen
                ? DisplayServer.WindowMode.Fullscreen
                : DisplayServer.WindowMode.Windowed);
            if (!Current.Fullscreen)
            {
                DisplayServer.WindowSetSize(new Vector2I(
                    Math.Max(960, Current.ResolutionWidth),
                    Math.Max(540, Current.ResolutionHeight)));
            }
        }
        GetTree().Root.Scaling3DScale = Current.QualityLevel switch
        {
            <= 0 => 0.75f,
            1 => 0.9f,
            _ => 1.0f
        };
    }

    private static void EnsureAudioBus(string name)
    {
        if (AudioServer.GetBusIndex(name) >= 0)
        {
            return;
        }
        AudioServer.AddBus();
        AudioServer.SetBusName(AudioServer.BusCount - 1, name);
    }

    private static void ApplyBusVolume(string name, float volume)
    {
        int bus = AudioServer.GetBusIndex(name);
        if (bus < 0)
        {
            return;
        }
        float linear = Math.Clamp(volume, 0.0001f, 1.0f);
        AudioServer.SetBusVolumeDb(bus, Mathf.LinearToDb(linear));
    }
}
