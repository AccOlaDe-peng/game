using Catalyst.Boss;
using Godot;

namespace Catalyst.Presentation;

/// <summary>
/// Plays a scene's background music on the "Music" bus in a seamless loop.
/// MusicPath is relative to res://assets/audio/music/ (e.g. "Background.wav").
/// When BossMusicPath is set and a BossController exists in the scene, the
/// track swaps to the boss cue while the boss is alive and back on defeat.
/// Menu scenes have no boss node, so they just loop the main cue.
/// </summary>
public partial class BackgroundMusicSystem : Node
{
    [Export]
    public string MusicPath { get; set; } = string.Empty;

    [Export]
    public string BossMusicPath { get; set; } = string.Empty;

    private AudioStreamPlayer _player = null!;
    private AudioStream? _mainStream;
    private AudioStream? _bossStream;

    public override void _Ready()
    {
        _mainStream = Load(MusicPath);
        if (_mainStream is null)
        {
            GD.PushWarning($"BackgroundMusicSystem: no stream for '{MusicPath}'.");
            return;
        }
        _bossStream = Load(BossMusicPath);

        _player = new AudioStreamPlayer
        {
            Bus = "Music",
            VolumeDb = -8.0f,
        };
        _player.Finished += Loop;
        AddChild(_player);

        if (GetNodeOrNull("../SimulationRoot/BossController") is BossController boss)
        {
            boss.BossSpawned += OnBossSpawned;
            boss.BossDefeated += OnBossDefeated;
        }

        _player.Stream = _mainStream;
        _player.Play();
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_player))
        {
            _player.Finished -= Loop;
        }
        if (GetNodeOrNull("../SimulationRoot/BossController") is BossController boss)
        {
            boss.BossSpawned -= OnBossSpawned;
            boss.BossDefeated -= OnBossDefeated;
        }
    }

    private AudioStream? Load(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }
        return ResourceLoader.Load<AudioStream>($"res://assets/audio/music/{path}");
    }

    private void OnBossSpawned(Catalyst.Enemies.EntityHandle handle)
    {
        if (_bossStream is not null && IsInstanceValid(_player))
        {
            _player.Stream = _bossStream;
            _player.Play();
        }
    }

    private void OnBossDefeated()
    {
        if (IsInstanceValid(_player))
        {
            _player.Stream = _mainStream;
            _player.Play();
        }
    }

    private void Loop()
    {
        if (IsInstanceValid(_player))
        {
            _player.Play();
        }
    }
}