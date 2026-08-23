using Catalyst.App;
using Godot;

namespace Catalyst.UI;

public partial class SettingsScreen : Control
{
    [Signal]
    public delegate void ClosedEventHandler();

    private SettingsService _settings = null!;
    private HSlider _master = null!;
    private HSlider _music = null!;
    private HSlider _effects = null!;
    private HSlider _shake = null!;
    private OptionButton _resolution = null!;
    private OptionButton _quality = null!;
    private OptionButton _fps = null!;
    private CheckButton _fullscreen = null!;
    private CheckButton _damageNumbers = null!;
    private CheckButton _elementAssist = null!;

    private static readonly Vector2I[] Resolutions =
    {
        new(1280, 720), new(1600, 900), new(1920, 1080), new(2560, 1440)
    };
    private static readonly int[] FrameRates = { 30, 60, 120, 144, 0 };

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always;
        _settings = GetNode<SettingsService>("/root/SettingsService");
        _master = GetNode<HSlider>("%MasterSlider");
        _music = GetNode<HSlider>("%MusicSlider");
        _effects = GetNode<HSlider>("%EffectsSlider");
        _shake = GetNode<HSlider>("%ShakeSlider");
        _resolution = GetNode<OptionButton>("%ResolutionOption");
        _quality = GetNode<OptionButton>("%QualityOption");
        _fps = GetNode<OptionButton>("%FpsOption");
        _fullscreen = GetNode<CheckButton>("%FullscreenToggle");
        _damageNumbers = GetNode<CheckButton>("%DamageNumbersToggle");
        _elementAssist = GetNode<CheckButton>("%ElementAssistToggle");

        foreach (Vector2I size in Resolutions)
        {
            _resolution.AddItem($"{size.X} × {size.Y}");
        }
        _quality.AddItem("性能");
        _quality.AddItem("平衡");
        _quality.AddItem("高质量");
        foreach (int rate in FrameRates)
        {
            _fps.AddItem(rate == 0 ? "不限制" : $"{rate} FPS");
        }

        GetNode<Button>("%ApplyButton").Pressed += Apply;
        GetNode<Button>("%CancelButton").Pressed += Close;
        Visible = false;
    }

    public void Open()
    {
        AppSettings value = _settings.Current;
        _master.Value = value.MasterVolume;
        _music.Value = value.MusicVolume;
        _effects.Value = value.EffectsVolume;
        _shake.Value = value.CameraShakeStrength;
        _fullscreen.ButtonPressed = value.Fullscreen;
        _damageNumbers.ButtonPressed = value.ShowDamageNumbers;
        _elementAssist.ButtonPressed = value.ElementIconAssist;
        _quality.Select(Math.Clamp(value.QualityLevel, 0, 2));
        int resolutionIndex = Array.FindIndex(Resolutions,
            size => size.X == value.ResolutionWidth && size.Y == value.ResolutionHeight);
        _resolution.Select(Math.Max(0, resolutionIndex));
        int fpsIndex = Array.IndexOf(FrameRates, value.FrameRateLimit);
        _fps.Select(Math.Max(0, fpsIndex));
        Visible = true;
        GetNode<Button>("%ApplyButton").GrabFocus();
    }

    private void Apply()
    {
        AppSettings value = _settings.Current;
        Vector2I resolution = Resolutions[_resolution.Selected];
        value.MasterVolume = (float)_master.Value;
        value.MusicVolume = (float)_music.Value;
        value.EffectsVolume = (float)_effects.Value;
        value.CameraShakeStrength = (float)_shake.Value;
        value.Fullscreen = _fullscreen.ButtonPressed;
        value.ShowDamageNumbers = _damageNumbers.ButtonPressed;
        value.ElementIconAssist = _elementAssist.ButtonPressed;
        value.ResolutionWidth = resolution.X;
        value.ResolutionHeight = resolution.Y;
        value.QualityLevel = _quality.Selected;
        value.FrameRateLimit = FrameRates[_fps.Selected];
        _settings.ApplyAndSave();
        Close();
    }

    private void Close()
    {
        Visible = false;
        EmitSignal(SignalName.Closed);
    }
}
