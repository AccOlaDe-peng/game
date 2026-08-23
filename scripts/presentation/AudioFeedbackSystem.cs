using Catalyst.Boss;
using Catalyst.Elements;
using Catalyst.Player;
using Godot;

namespace Catalyst.Presentation;

public partial class AudioFeedbackSystem : Node
{
    private const float MixRate = 22050.0f;
    private AudioStreamPlayer _player = null!;
    private AudioStreamGeneratorPlayback? _playback;
    private ElementSystem _elements = null!;
    private PlayerHealth _health = null!;
    private BossController _boss = null!;
    private float _previousHealth;

    public override void _Ready()
    {
        _elements = GetNode<ElementSystem>("../ElementSystem");
        _health = GetNode<PlayerHealth>("../../WorldRoot/Player/HealthComponent");
        _boss = GetNode<BossController>("../BossController");
        _player = new AudioStreamPlayer
        {
            Bus = "SFX",
            Stream = new AudioStreamGenerator
            {
                MixRate = MixRate,
                BufferLength = 0.35f
            }
        };
        AddChild(_player);
        _player.Play();
        _playback = _player.GetStreamPlayback() as AudioStreamGeneratorPlayback;
        _previousHealth = _health.CurrentHealth;
        _elements.ReactionTriggered += OnReactionTriggered;
        _health.HealthChanged += OnHealthChanged;
        _boss.BossSpawned += OnBossSpawned;
        _boss.BossDefeated += OnBossDefeated;
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_elements))
        {
            _elements.ReactionTriggered -= OnReactionTriggered;
        }
        if (IsInstanceValid(_health))
        {
            _health.HealthChanged -= OnHealthChanged;
        }
        if (IsInstanceValid(_boss))
        {
            _boss.BossSpawned -= OnBossSpawned;
            _boss.BossDefeated -= OnBossDefeated;
        }
    }

    private void OnReactionTriggered(ReactionKind kind, Vector2 position, float damage)
    {
        PlayTone(kind switch
        {
            ReactionKind.SteamShock => 520.0f,
            ReactionKind.Conduction => 760.0f,
            _ => 440.0f
        }, 0.08f, 0.16f);
    }

    private void OnHealthChanged(float current, float maximum)
    {
        if (current < _previousHealth)
        {
            PlayTone(115.0f, 0.07f, 0.13f);
        }
        _previousHealth = current;
    }

    private void OnBossSpawned(Catalyst.Enemies.EntityHandle handle) =>
        PlayTone(82.0f, 0.18f, 0.22f);

    private void OnBossDefeated() => PlayTone(660.0f, 0.22f, 0.18f);

    private void PlayTone(float frequency, float duration, float volume)
    {
        if (_playback is null)
        {
            return;
        }
        int frames = Mathf.RoundToInt(MixRate * duration);
        for (int index = 0; index < frames && _playback.GetFramesAvailable() > 0; index++)
        {
            float progress = index / (float)Math.Max(1, frames - 1);
            float envelope = Mathf.Sin(progress * Mathf.Pi);
            float sample = Mathf.Sin(Mathf.Tau * frequency * index / MixRate) * volume * envelope;
            _playback.PushFrame(new Vector2(sample, sample));
        }
    }
}
