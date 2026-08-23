using Catalyst.Core;
using Catalyst.Enemies;
using Catalyst.Run;
using Godot;
using Catalyst.App;

namespace Catalyst.Waves;

public partial class WaveDirector : Node
{
    [Export(PropertyHint.Range, "0,1000,1")]
    public int EnemyLimit { get; set; } = 500;

    [Export] public float SpawnRadius { get; set; } = 28.0f;
    [Export] public float RunDuration { get; set; } = 720.0f;

    private readonly List<EnemyDefinition> _enemyDefinitions = new();
    private readonly List<WaveDefinition> _phases = new();
    private readonly List<EnemyDefinition> _available = new(8);
    private EnemySystem _enemies = null!;
    private RunController _run = null!;
    private CharacterBody3D _player = null!;
    private float _spawnRemaining;
    private double _nextEliteAt = 180.0;
    private float _eventPressure = 1.0f;

    public float EventPressure => _eventPressure;

    public override void _Ready()
    {
        _enemies = GetNode<EnemySystem>("../EntitySystem");
        _run = GetNode<RunController>("../RunController");
        _player = GetNode<CharacterBody3D>("../../WorldRoot/Player");
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
        if (EnemyLimit <= 0 || _enemies.ActiveCount >= EnemyLimit)
        {
            return;
        }

        _spawnRemaining -= (float)deltaValue;
        if (_spawnRemaining > 0.0f)
        {
            return;
        }

        WaveDefinition phase = GetCurrentPhase();
        int batchSize = Math.Max(1, Mathf.RoundToInt(phase.BatchSize * _eventPressure));
        for (int index = 0; index < batchSize && _enemies.ActiveCount < EnemyLimit; index++)
        {
            EnemyDefinition? definition = SelectDefinition(
                allowElite: _run.ElapsedSeconds >= _nextEliteAt && index == 0);
            if (definition is null)
            {
                break;
            }
            _enemies.Spawn(GetSpawnPosition(), definition);
            if (definition.IsElite)
            {
                _nextEliteAt = _run.ElapsedSeconds + 75.0;
                CatalystLog.Info("Waves", $"Elite spawned: {definition.DisplayName}.");
            }
        }

        _spawnRemaining = Math.Max(
            0.06f,
            phase.SpawnInterval / (phase.ThreatMultiplier * _eventPressure));
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
