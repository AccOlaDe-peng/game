using Catalyst.Core;
using Catalyst.Enemies;
using Catalyst.Run;
using Godot;
using Catalyst.App;

namespace Catalyst.Waves;

public partial class WaveDirector : Node
{
    public event Action<string>? EliteAnnounced;

    [Export(PropertyHint.Range, "0,1000,1")]
    public int EnemyLimit { get; set; } = 500;

    [Export] public float SpawnRadius { get; set; } = 28.0f;
    [Export] public float RunDuration { get; set; } = 720.0f;

    // Continuous difficulty curve (no hard phase steps):
    // spawn interval decays smoothly, batch size grows every 3 minutes and
    // enemy health scales mildly so late kills stay meaningful.
    [Export] public float InitialSpawnInterval { get; set; } = 1.0f;
    [Export] public float MinimumSpawnInterval { get; set; } = 0.18f;
    [Export] public float IntervalDecayPerMinute { get; set; } = 0.9f;
    [Export] public float FirstEliteAtSeconds { get; set; } = 240.0f;
    [Export] public float EliteRespawnIntervalSeconds { get; set; } = 75.0f;
    [Export] public float EliteAnnounceLeadSeconds { get; set; } = 5.0f;
    [Export] public float HealthScalePerMinute { get; set; } = 0.05f;

    private readonly List<EnemyDefinition> _enemyDefinitions = new();
    private readonly List<WaveDefinition> _phases = new();
    private readonly List<EnemyDefinition> _available = new(8);
    private EnemySystem _enemies = null!;
    private RunController _run = null!;
    private CharacterBody3D _player = null!;
    private float _spawnRemaining;
    private double _nextEliteAt;
    private double _eliteAnnounceRemaining = -1.0;
    private bool _eliteAnnounced;
    private float _eventPressure = 1.0f;

    public float EventPressure => _eventPressure;

    public override void _Ready()
    {
        _enemies = GetNode<EnemySystem>("../EntitySystem");
        _run = GetNode<RunController>("../RunController");
        _player = GetNode<CharacterBody3D>("../../WorldRoot/Player");
        _nextEliteAt = FirstEliteAtSeconds;
        LoadDefinitions();
    }

    public override void _PhysicsProcess(double deltaValue)
    {
        if (_run.State != RunState.Playing)
        {
            return;
        }
        if (_run.ElapsedSeconds >= RunDuration)
        {
            return;
        }
        UpdateEliteAnnouncement(deltaValue);
        if (EnemyLimit <= 0 || _enemies.ActiveCount >= EnemyLimit)
        {
            return;
        }

        _spawnRemaining -= (float)deltaValue;
        if (_spawnRemaining > 0.0f)
        {
            return;
        }

        int batchSize = CurrentBatchSize();
        for (int index = 0; index < batchSize && _enemies.ActiveCount < EnemyLimit; index++)
        {
            bool eliteDue = _run.ElapsedSeconds >= _nextEliteAt;
            EnemyDefinition? definition = SelectDefinition(
                allowElite: eliteDue && index == 0);
            if (definition is null)
            {
                break;
            }
            _enemies.Spawn(GetSpawnPosition(), definition, CurrentHealthMultiplier());
            if (definition.IsElite)
            {
                _nextEliteAt = _run.ElapsedSeconds + EliteRespawnIntervalSeconds;
                _eliteAnnounced = false;
                CatalystLog.Info("Waves", $"Elite spawned: {definition.DisplayName}.");
            }
        }

        _spawnRemaining = Math.Max(
            MinimumSpawnInterval,
            CurrentSpawnInterval() / Mathf.Max(1.0f, _eventPressure));
    }

    /// <summary>Smooth spawn interval: 1.0s decaying 10% per minute, floor 0.18s.</summary>
    public float CurrentSpawnInterval()
    {
        float decay = Mathf.Pow(IntervalDecayPerMinute, (float)(_run.ElapsedSeconds / 60.0));
        return Math.Max(MinimumSpawnInterval, InitialSpawnInterval * decay);
    }

    /// <summary>Batch size grows by 1 every 180 s, capped at 4.</summary>
    public int CurrentBatchSize() =>
        Math.Min(4, 1 + (int)(_run.ElapsedSeconds / 180.0));

    /// <summary>Mild late-game health scaling: +5% per minute.</summary>
    public float CurrentHealthMultiplier() =>
        1.0f + (float)(_run.ElapsedSeconds / 60.0) * HealthScalePerMinute;

    private void UpdateEliteAnnouncement(double delta)
    {
        if (_eliteAnnounced && _eliteAnnounceRemaining <= 0.0)
        {
            return;
        }
        double secondsUntilElite = _nextEliteAt - _run.ElapsedSeconds;
        if (!_eliteAnnounced && secondsUntilElite <= EliteAnnounceLeadSeconds &&
            secondsUntilElite > 0.0 && _nextEliteAt < RunDuration)
        {
            _eliteAnnounced = true;
            _eliteAnnounceRemaining = EliteAnnounceLeadSeconds;
            EliteAnnounced?.Invoke("警告：灾变核心前兆正在接近");
            CatalystLog.Info("Waves", "Elite spawn announced.");
        }
        _eliteAnnounceRemaining = Math.Max(-1.0, _eliteAnnounceRemaining - delta);
    }

    public void SetEventPressure(float multiplier)
    {
        _eventPressure = Mathf.Clamp(multiplier, 1.0f, 2.5f);
    }

    private void LoadDefinitions()
    {
        ContentCatalog catalog = GetNode<ContentCatalog>("/root/ContentCatalog");
        foreach (EnemyDefinition definition in catalog.All<EnemyDefinition>())
        {
            _enemyDefinitions.Add(definition);
        }
        foreach (WaveDefinition definition in catalog.All<WaveDefinition>())
        {
            _phases.Add(definition);
        }
        _phases.Sort((left, right) => left.StartsAtSeconds.CompareTo(right.StartsAtSeconds));
        if (_enemyDefinitions.Count != 8 || _phases.Count != 4)
        {
            CatalystLog.Error("Waves",
                $"Content load incomplete: {_enemyDefinitions.Count}/8 enemies, " +
                $"{_phases.Count}/4 phases.");
        }
    }

    private WaveDefinition GetCurrentPhase()
    {
        WaveDefinition current = _phases[0];
        foreach (WaveDefinition phase in _phases)
        {
            if (_run.ElapsedSeconds < phase.StartsAtSeconds)
            {
                break;
            }
            current = phase;
        }
        return current;
    }

    private EnemyDefinition? SelectDefinition(bool allowElite)
    {
        _available.Clear();
        foreach (EnemyDefinition definition in _enemyDefinitions)
        {
            if (definition.AppearsAfterSeconds > _run.ElapsedSeconds ||
                _enemies.CountArchetype(definition.Archetype) >= definition.MaximumAlive)
            {
                continue;
            }
            if (allowElite != definition.IsElite)
            {
                continue;
            }
            _available.Add(definition);
        }

        if (_available.Count == 0 && allowElite)
        {
            return SelectDefinition(false);
        }
        if (_available.Count == 0)
        {
            return null;
        }

        float totalWeight = _available.Sum(definition =>
            definition.SpawnWeight / Math.Max(0.1f, definition.SpawnCost));
        float roll = _run.RandomStreams.Wave.RandfRange(0.0f, totalWeight);
        foreach (EnemyDefinition definition in _available)
        {
            roll -= definition.SpawnWeight / Math.Max(0.1f, definition.SpawnCost);
            if (roll <= 0.0f)
            {
                return definition;
            }
        }
        return _available[^1];
    }

    private Vector2 GetSpawnPosition()
    {
        Vector2 player = new(_player.GlobalPosition.X, _player.GlobalPosition.Z);
        float angle = _run.RandomStreams.SpawnPosition.RandfRange(0.0f, Mathf.Tau);
        float radius = _run.RandomStreams.SpawnPosition.RandfRange(
            SpawnRadius - 3.0f,
            SpawnRadius + 3.0f);
        Vector2 position = player + Vector2.FromAngle(angle) * radius;
        return new Vector2(
            Mathf.Clamp(position.X, -38.0f, 38.0f),
            Mathf.Clamp(position.Y, -38.0f, 38.0f));
    }
}
