using Catalyst.App;
using Catalyst.Combat;
using Catalyst.Elements;
using Catalyst.Enemies;
using Catalyst.Run;
using Catalyst.Spatial;
using Godot;

namespace Catalyst.Player;

public partial class CatalyzeAbility : Node
{
    private static readonly StringName CatalyzeId = new("ability.element_catalyze");

    [Export] public float Cooldown { get; set; } = 5.0f;
    [Export] public float Radius { get; set; } = 4.5f;
    [Export] public float TargetDistance { get; set; } = 9.0f;
    [Export] public float BaseDamage { get; set; } = 1.5f;

    public event Action<Vector2, float>? Catalyzed;

    private readonly List<EntityHandle> _targets = new(128);
    private CharacterBody3D _player = null!;
    private PlayerController _controller = null!;
    private Camera3D _camera = null!;
    private SpatialGrid _grid = null!;
    private CombatSystem _combat = null!;
    private RunController _run = null!;
    private Vector2 _lastControllerAim = Vector2.Up;
    private bool _dodgeRangeBonus;

    public float CooldownRemaining { get; private set; }
    public float CooldownFraction => Cooldown <= 0.0f ? 0.0f : CooldownRemaining / Cooldown;

    public override void _Ready()
    {
        _player = GetParent<CharacterBody3D>();
        _controller = GetParent<PlayerController>();
        _camera = GetNode<Camera3D>("../SpringArm3D/Camera3D");
        _grid = GetNode<SpatialGrid>("../../../SimulationRoot/SpatialGrid");
        _combat = GetNode<CombatSystem>("../../../SimulationRoot/CombatSystem");
        _run = GetNode<RunController>("../../../SimulationRoot/RunController");
        _controller.Dodged += OnDodged;
    }

    public override void _PhysicsProcess(double deltaValue)
    {
        if (_run.State != RunState.Playing)
        {
            return;
        }

        CooldownRemaining = Math.Max(0.0f, CooldownRemaining - (float)deltaValue);
        Vector2 aim = Input.GetVector(
            InputBootstrap.AimLeft,
            InputBootstrap.AimRight,
            InputBootstrap.AimUp,
            InputBootstrap.AimDown);
        if (aim.LengthSquared() > 0.16f)
        {
            _lastControllerAim = aim.Normalized();
        }

        if (Input.IsActionJustPressed(InputBootstrap.Catalyze) && CooldownRemaining <= 0.0f)
        {
            Vector2 target = aim.LengthSquared() > 0.16f
                ? GetPlayerPosition() + _lastControllerAim * TargetDistance
                : GetMouseTarget();
            TriggerAt(target);
        }
    }

    public int TriggerAt(Vector2 target)
    {
        if (_run.State != RunState.Playing || CooldownRemaining > 0.0f)
        {
            return 0;
        }

        float actualRadius = Radius * (_dodgeRangeBonus ? 1.35f : 1.0f);
        _dodgeRangeBonus = false;
        _grid.QueryCircle(target, actualRadius, _targets);
        foreach (EntityHandle enemy in _targets)
        {
            _combat.Submit(new DamageContext(
                enemy,
                CatalyzeId,
                BaseDamage,
                ElementType.None,
                0,
                DamageFlags.ForceReactionCheck));
        }

        CooldownRemaining = Cooldown;
        Catalyzed?.Invoke(target, actualRadius);
        return _targets.Count;
    }

    public void ResetCooldown() => CooldownRemaining = 0.0f;

    public override void _ExitTree()
    {
        if (IsInstanceValid(_controller))
        {
            _controller.Dodged -= OnDodged;
        }
    }

    private Vector2 GetMouseTarget()
    {
        Vector2 mousePosition = GetViewport().GetMousePosition();
        Vector3 origin = _camera.ProjectRayOrigin(mousePosition);
        Vector3 direction = _camera.ProjectRayNormal(mousePosition);
        if (Mathf.Abs(direction.Y) <= 0.0001f)
        {
            return GetPlayerPosition() + _lastControllerAim * TargetDistance;
        }

        float distance = -origin.Y / direction.Y;
        Vector3 world = origin + direction * Math.Max(0.0f, distance);
        Vector2 target = new(world.X, world.Z);
        Vector2 playerPosition = GetPlayerPosition();
        Vector2 offset = target - playerPosition;
        return offset.Length() > TargetDistance
            ? playerPosition + offset.Normalized() * TargetDistance
            : target;
    }

    private Vector2 GetPlayerPosition() => new(_player.GlobalPosition.X, _player.GlobalPosition.Z);
    private void OnDodged() => _dodgeRangeBonus = true;
}
