namespace Catalyst.App;

public sealed class AppSettings
{
    public int SchemaVersion { get; set; } = 2;
    public float MasterVolume { get; set; } = 1.0f;
    public float MusicVolume { get; set; } = 0.8f;
    public float EffectsVolume { get; set; } = 0.9f;
    public float CameraShakeStrength { get; set; } = 1.0f;
    public bool ShowDamageNumbers { get; set; } = true;
    public bool ElementIconAssist { get; set; } = true;
    public bool Fullscreen { get; set; }
    public int ResolutionWidth { get; set; } = 1280;
    public int ResolutionHeight { get; set; } = 720;
    public int QualityLevel { get; set; } = 1;
    public int FrameRateLimit { get; set; } = 60;
}
