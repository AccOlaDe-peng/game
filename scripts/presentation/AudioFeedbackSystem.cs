using Catalyst.Boss;
using Catalyst.Elements;
using Catalyst.Pickups;
using Catalyst.Player;
using Catalyst.Spells;
using Godot;

namespace Catalyst.Presentation;

/// <summary>
/// Plays Kenney SFX for combat, reaction, boss, spell and pickup events.
/// Each cue picks a random variant from a preloaded pool with slight pitch
/// jitter so repeated events stay organic instead of sounding robotic.
/// </summary>
public partial class AudioFeedbackSystem : Node
{
    private const int PoolSize = 8;

    private readonly List<AudioStreamPlayer> _pool = new(PoolSize);
    private int _nextPlayer;
    private readonly Random _random = new();
    private ElementSystem _elements = null!;
    private PlayerHealth _health = null!;
    private BossController _boss = null!;
    private SpellSystem _spells = null!;
    private PickupSystem _pickups = null!;
    private float _previousHealth;

    private AudioStream[] _steamBlast = Array.Empty<AudioStream>();
    private AudioStream[] _electricBlast = Array.Empty<AudioStream>();
    private AudioStream[] _arcaneBlast = Array.Empty<AudioStream>();
    private AudioStream[] _playerHit = Array.Empty<AudioStream>();
    private AudioStream[] _bossSpawn = Array.Empty<AudioStream>();
    private AudioStream[] _bossDefeated = Array.Empty<AudioStream>();
    private AudioStream[] _castBlip = Array.Empty<AudioStream>();
    private AudioStream[] _castElectric = Array.Empty<AudioStream>();
    private AudioStream[] _discSlash = Array.Empty<AudioStream>();
    private AudioStream[] _orbPop = Array.Empty<AudioStream>();
    private AudioStream[] _mineThump = Array.Empty<AudioStream>();
    private AudioStream[] _pickupCoin = Array.Empty<AudioStream>();

    public override void _Ready()
    {
        _elements = GetNode<ElementSystem>("../ElementSystem");
        _health = GetNode<PlayerHealth>("../../WorldRoot/Player/HealthComponent");
        _boss = GetNode<BossController>("../BossController");
        _spells = GetNode<SpellSystem>("../SpellSystem");
        _pickups = GetNode<PickupSystem>("../PickupSystem");

        _steamBlast = Load(
            "sfx/impactSoft_heavy_000", "sfx/impactSoft_heavy_001", "sfx/impactSoft_heavy_002",
            "sfx/impactSoft_heavy_003", "sfx/impactSoft_heavy_004");
        _electricBlast = Load(
            "sfx/impactGlass_heavy_000", "sfx/impactGlass_heavy_001", "sfx/impactGlass_heavy_002",
            "sfx/impactGlass_heavy_003", "sfx/impactGlass_heavy_004");
        _arcaneBlast = Load(
            "sfx/impactMetal_heavy_000", "sfx/impactMetal_heavy_001", "sfx/impactMetal_heavy_002",
            "sfx/impactMetal_heavy_003", "sfx/impactMetal_heavy_004");
        _playerHit = Load(
            "sfx/impactPunch_medium_000", "sfx/impactPunch_medium_001", "sfx/impactPunch_medium_002",
            "sfx/impactPunch_medium_003", "sfx/impactPunch_medium_004");
        _bossSpawn = Load(
            "sfx/impactBell_heavy_000", "sfx/impactBell_heavy_001", "sfx/impactBell_heavy_002",
            "sfx/impactBell_heavy_003", "sfx/impactBell_heavy_004");
        _bossDefeated = Load(
            "ui/confirmation_001", "ui/confirmation_002", "ui/confirmation_003",
            "ui/confirmation_004");
        _castBlip = Load(
            "ui/select_001", "ui/select_002", "ui/select_003", "ui/select_004",
            "ui/select_005", "ui/select_006", "ui/select_007", "ui/select_008");
        _castElectric = Load(
            "sfx/impactGlass_medium_000", "sfx/impactGlass_medium_001", "sfx/impactGlass_medium_002",
            "sfx/impactGlass_medium_003", "sfx/impactGlass_medium_004");
        _discSlash = Load("rpg/knifeSlice", "rpg/knifeSlice2");
        _orbPop = Load("ui/pluck_001", "ui/pluck_002");
        _mineThump = Load(
            "sfx/impactSoft_medium_000", "sfx/impactSoft_medium_001", "sfx/impactSoft_medium_002",
            "sfx/impactSoft_medium_003", "sfx/impactSoft_medium_004");
        _pickupCoin = Load("rpg/handleCoins", "rpg/handleCoins2");

        _previousHealth = _health.CurrentHealth;
        _elements.ReactionTriggered += OnReactionTriggered;
        _health.HealthChanged += OnHealthChanged;
        _boss.BossSpawned += OnBossSpawned;
        _boss.BossDefeated += OnBossDefeated;
        _spells.SpellCast += OnSpellCast;
        _pickups.PickupCollected += OnPickupCollected;
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
        if (IsInstanceValid(_spells))
        {
            _spells.SpellCast -= OnSpellCast;
        }
        if (IsInstanceValid(_pickups))
        {
            _pickups.PickupCollected -= OnPickupCollected;
        }
        foreach (AudioStreamPlayer player in _pool)
        {
            player.Stop();
            if (IsInstanceValid(player))
            {
                if (player.IsInsideTree())
                {
                    RemoveChild(player);
                }
                player.Free();
            }
        }
        _pool.Clear();
    }

    private void OnReactionTriggered(ReactionKind kind, Vector2 position, float damage)
    {
        switch (kind)
        {
            case ReactionKind.SteamShock:
                PlayVariants(_steamBlast, -8.0f, 0.9f, 1.1f);
                break;
            case ReactionKind.Conduction:
                PlayVariants(_electricBlast, -6.0f, 1.0f, 1.2f);
                break;
            default:
                PlayVariants(_arcaneBlast, -6.0f, 0.95f, 1.1f);
                break;
        }
    }

    private void OnHealthChanged(float current, float maximum)
    {
        if (current < _previousHealth)
        {
            PlayVariants(_playerHit, -4.0f, 0.9f, 1.1f);
        }
        _previousHealth = current;
    }

    private void OnBossSpawned(Catalyst.Enemies.EntityHandle handle) =>
        PlayVariants(_bossSpawn, 0.0f, 0.85f, 1.0f);

    private void OnBossDefeated() => PlayVariants(_bossDefeated, -4.0f, 1.0f, 1.0f);

    private void OnSpellCast(SpellCastKind kind)
    {
        switch (kind)
        {
            case SpellCastKind.ChainLightning:
                PlayVariants(_castElectric, -12.0f, 1.0f, 1.2f);
                break;
            case SpellCastKind.RicochetDisc:
                PlayVariants(_discSlash, -10.0f, 1.0f, 1.2f);
                break;
            case SpellCastKind.OrbitOrb:
                PlayVariants(_orbPop, -12.0f, 1.0f, 1.3f);
                break;
            case SpellCastKind.ElementMine:
                PlayVariants(_mineThump, -12.0f, 0.9f, 1.1f);
                break;
            default:
                PlayVariants(_castBlip, -14.0f, 1.0f, 1.4f);
                break;
        }
    }

    private void OnPickupCollected() => PlayVariants(_pickupCoin, -14.0f, 0.95f, 1.25f);

    private static AudioStream[] Load(params string[] paths)
    {
        var streams = new List<AudioStream>(paths.Length);
        foreach (string path in paths)
        {
            AudioStream? stream = ResourceLoader.Load<AudioStream>($"res://assets/audio/{path}.ogg");
            if (stream is not null)
            {
                streams.Add(stream);
            }
        }
        return streams.ToArray();
    }

    private void PlayVariants(AudioStream[] variants, float volumeDb, float pitchMin, float pitchMax)
    {
        if (variants.Length == 0)
        {
            return;
        }
        AudioStreamPlayer player = NextPlayer();
        player.Stream = variants[_random.Next(variants.Length)];
        player.VolumeDb = volumeDb;
        player.PitchScale = (float)(pitchMin + _random.NextDouble() * (pitchMax - pitchMin));
        player.Play();
    }

    private AudioStreamPlayer NextPlayer()
    {
        if (_pool.Count < PoolSize)
        {
            AudioStreamPlayer player = new() { Bus = "SFX" };
            AddChild(player);
            _pool.Add(player);
        }
        AudioStreamPlayer next = _pool[_nextPlayer];
        _nextPlayer = (_nextPlayer + 1) % _pool.Count;
        return next;
    }
}
