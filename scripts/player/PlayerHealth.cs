using Catalyst.Run;
using Catalyst.Spatial;
using Catalyst.Enemies;
using Godot;

namespace Catalyst.Player;

public partial class PlayerHealth : Node
{
    [Export]
    public float MaxHealth { get; set; } = 100.0f;

    [Export]
    public float ContactDamage { get; set; } = 5.0f;

    [Export]
    public float ContactInterval { get; set; } = 0.8f;

    [Export]
    public float ContactRadius { get; set; } = 1.05f;

    /// <summary>Brief invulnerability after any hit so overlapping enemies and
    /// projectiles cannot stack lethal damage in the same instant.</summary>
    [Export]
    public float InvulnerabilitySeconds { get; set; } = 0.35f;

    public event Action<float, float>? HealthChanged;

    private CharacterBody3D _player = null!;
    private SpatialGrid _grid = null!;
    private RunController _run = null!;
    private EnemySystem _enemies = null!;
    private RunStatistics _statistics = null!;
    private float _contactRemaining;
    private float _invulnerabilityRemaining;

    public float CurrentHealth { get; private set; }

    public override void _Ready()
    {
        _player = GetParent<CharacterBody3D>();
        _grid = GetNode<SpatialGrid>("../../../SimulationRoot/SpatialGrid");
        _run = GetNode<RunController>("../../../SimulationRoot/RunController");
        _enemies = GetNode<EnemySystem>("../../../SimulationRoot/EntitySystem");
        _statistics = GetNode<RunStatistics>("../../../SimulationRoot/RunStatistics");
        CurrentHealth = MaxHealth;
    }

    public override void _PhysicsProcess(double deltaValue)
    {
        if (_run.State != RunState.Playing)
        {
            return;
        }

        _contactRemaining = Math.Max(0.0f, _contactRemaining - (float)deltaValue);
        _invulnerabilityRemaining = Math.Max(0.0f, _invulnerabilityRemaining - (float)deltaValue);
        if (_contactRemaining > 0.0f)
        {
            return;
        }

        Vector2 position = new(_player.GlobalPosition.X, _player.GlobalPosition.Z);
        if (_grid.TryFindNearest(position, ContactRadius, out var enemyHandle))
        {
            float damage = _enemies.TryGet(enemyHandle, out var enemy)
                ? enemy.ContactDamage
                : ContactDamage;
            ApplyDamage(damage, "敌人接触");
            _contactRemaining = ContactInterval;
        }
    }

    public void ApplyDamage(float amount, string cause = "敌人攻击")
    {
        if (amount <= 0.0f || CurrentHealth <= 0.0f ||
            (_invulnerabilityRemaining > 0.0f && _run.State == RunState.Playing))
        {
            return;
        }

        float resolvedDamage = Math.Min(CurrentHealth, amount);
        CurrentHealth = Math.Max(0.0f, CurrentHealth - amount);
        _invulnerabilityRemaining = InvulnerabilitySeconds;
        _statistics.RecordDamageTaken(resolvedDamage, cause);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
        if (CurrentHealth <= 0.0f)
        {
            _run.EnterDefeat();
        }
    }

    /// <summary>Restores a fraction of maximum health (level-up reward).</summary>
    public void RestoreFraction(float fraction)
    {
        if (fraction <= 0.0f || CurrentHealth <= 0.0f || CurrentHealth >= MaxHealth)
        {
            return;
        }
        CurrentHealth = Math.Min(MaxHealth, CurrentHealth + MaxHealth * fraction);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }

    public void IncreaseMaximumHealth(float multiplier)
    {
        if (multiplier <= 1.0f)
        {
            return;
        }

        float increase = MaxHealth * (multiplier - 1.0f);
        MaxHealth += increase;
        CurrentHealth = Math.Min(MaxHealth, CurrentHealth + increase);
        HealthChanged?.Invoke(CurrentHealth, MaxHealth);
    }
}
