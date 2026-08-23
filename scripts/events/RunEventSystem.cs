using Catalyst.Core;
using Catalyst.Player;
using Catalyst.Run;
using Catalyst.Upgrades;
using Catalyst.Waves;
using Catalyst.App;
using Godot;

namespace Catalyst.Events;

public partial class RunEventSystem : Node
{
    public event Action<string, float, bool>? ObjectiveChanged;

    private readonly Dictionary<RunEventKind, RunEventDefinition> _definitions = new();
    private RunController _run = null!;
    private WaveDirector _waves = null!;
    private PlayerProgression _progression = null!;
    private UpgradeSystem _upgrades = null!;
    private CharacterBody3D _player = null!;
    private Node3D _riftMarker = null!;
    private Node3D _circleMarker = null!;
    private RunEventDefinition? _active;
    private RunEventState _state;
    private Vector2 _position;
    private float _progress;
    private float _elapsed;
    private double _nextEventAt = 60.0;
    private int _eventSequence;
    private double _pressureResetAt = -1.0;

    public RunEventState State => _state;
    public RunEventKind? ActiveKind => _active?.Kind;
    public float ProgressFraction => _active is null
        ? 0.0f
        : Mathf.Clamp(_progress / _active.RequiredProgressSeconds, 0.0f, 1.0f);

    public override void _Ready()
    {
        _run = GetNode<RunController>("../RunController");
        _waves = GetNode<WaveDirector>("../WaveDirector");
        _progression = GetNode<PlayerProgression>("../../WorldRoot/Player/Progression");
        _upgrades = GetNode<UpgradeSystem>("../UpgradeSystem");
        _player = GetNode<CharacterBody3D>("../../WorldRoot/Player");
        _riftMarker = GetNode<Node3D>("../../WorldRoot/EventPresentation/SealingRift");
        _circleMarker = GetNode<Node3D>("../../WorldRoot/EventPresentation/StabilizationCircle");

        ContentCatalog catalog = GetNode<ContentCatalog>("/root/ContentCatalog");
        foreach (RunEventDefinition definition in catalog.All<RunEventDefinition>())
        {
            _definitions[definition.Kind] = definition;
        }
        SetMarkersVisible(false, false);
    }

    public override void _PhysicsProcess(double deltaValue)
    {
        if (_run.State != RunState.Playing)
        {
            return;
        }

        if (_state == RunEventState.Inactive)
        {
            if (_pressureResetAt > 0.0 && _run.ElapsedSeconds >= _pressureResetAt)
            {
                _waves.SetEventPressure(1.0f);
                _pressureResetAt = -1.0;
            }
            if (_run.ElapsedSeconds >= _nextEventAt)
            {
                StartEvent((RunEventKind)(_eventSequence++ % 2), GetRandomPosition());
            }
            return;
        }

        if (_state != RunEventState.Active || _active is null)
        {
            return;
        }

        float delta = (float)deltaValue;
        _elapsed += delta;
        Vector2 playerPosition = new(_player.GlobalPosition.X, _player.GlobalPosition.Z);
        bool inside = playerPosition.DistanceSquaredTo(_position) <= _active.Radius * _active.Radius;
        if (inside)
        {
            _progress += delta;
        }
        else
        {
            _progress = Math.Max(0.0f, _progress - _active.LeaveDecayPerSecond * delta);
        }

        ObjectiveChanged?.Invoke(
            $"{_active.DisplayName}  {(inside ? "稳定中" : "前往目标")}",
            ProgressFraction,
            true);

        if (_progress >= _active.RequiredProgressSeconds)
        {
            CompleteEvent(true);
        }
        else if (_elapsed >= _active.TimeLimitSeconds)
        {
            CompleteEvent(false);
        }
    }

    public bool StartEventForTests(RunEventKind kind, Vector2 position) => StartEvent(kind, position);

    private bool StartEvent(RunEventKind kind, Vector2 position)
    {
        if (_state != RunEventState.Inactive || !_definitions.TryGetValue(kind, out _active))
        {
            return false;
        }
        _position = position;
        _progress = 0.0f;
        _elapsed = 0.0f;
        _state = RunEventState.Active;
        _waves.SetEventPressure(_active.ActivePressureMultiplier);
        _pressureResetAt = -1.0;
        _riftMarker.Position = new Vector3(position.X, 0.05f, position.Y);
        _circleMarker.Position = new Vector3(position.X, 0.05f, position.Y);
        SetMarkersVisible(kind == RunEventKind.SealingRift, kind == RunEventKind.StabilizationCircle);
        ObjectiveChanged?.Invoke(_active.DisplayName, 0.0f, true);
        CatalystLog.Info("Events", $"Started {_active.Id} at {position}.");
        return true;
    }

    private void CompleteEvent(bool success)
    {
        if (_active is null)
        {
            return;
        }
        _state = success ? RunEventState.Succeeded : RunEventState.Failed;
        if (success)
        {
            _upgrades.RequestBonusUpgrade();
            _progression.AddExperience(_active.ExperienceReward);
        }
        else
        {
            _waves.SetEventPressure(1.35f);
            _pressureResetAt = _run.ElapsedSeconds + 30.0;
        }
        if (success)
        {
            _waves.SetEventPressure(1.0f);
        }
        ObjectiveChanged?.Invoke(
            success ? $"{_active.DisplayName}完成" : $"{_active.DisplayName}失败",
            success ? 1.0f : ProgressFraction,
            false);
        SetMarkersVisible(false, false);
        _nextEventAt = _run.ElapsedSeconds + 135.0;
        _active = null;
        _state = RunEventState.Inactive;
    }

    private Vector2 GetRandomPosition()
    {
        float angle = _run.RandomStreams.GameplayProc.RandfRange(0.0f, Mathf.Tau);
        float radius = _run.RandomStreams.GameplayProc.RandfRange(12.0f, 24.0f);
        return Vector2.FromAngle(angle) * radius;
    }

    private void SetMarkersVisible(bool rift, bool circle)
    {
        _riftMarker.Visible = rift;
        _circleMarker.Visible = circle;
    }
}
