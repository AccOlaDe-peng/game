using Catalyst.Core;
using Catalyst.Elements;
using Catalyst.Enemies;
using Catalyst.Player;
using Catalyst.Run;
using Godot;

namespace Catalyst.Boss;

public partial class BossController : Node
{
    public event Action<EntityHandle>? BossSpawned;
    public event Action? BossDefeated;
    public event Action<float, float>? StaggerChanged;

    [Export] public EnemyDefinition Definition { get; set; } = null!;
    [Export] public float SpawnTime { get; set; } = 720.0f;
    [Export] public float StaggerThreshold { get; set; } = 100.0f;

    private EnemySystem _enemies = null!;
    private ElementSystem _elements = null!;
    private RunController _run = null!;
    private CharacterBody3D _player = null!;
    private PlayerHealth _playerHealth = null!;
    private EnemyDefinition _guardDefinition = null!;
    private EntityHandle _boss = EntityHandle.Invalid;
    private BossAction _action;
    private Vector2 _lockedDirection;
    private float _actionRemaining;
    private float _nextSkillRemaining;
    private float _contactRemaining;
    private float _stagger;
    private int _skillIndex;
    private int _chargesRemaining;

    public bool HasSpawned { get; private set; }
    public bool IsDefeated { get; private set; }
    public EntityHandle BossHandle => _boss;
    public float Stagger => _stagger;
    public bool IsExposed => _action == BossAction.Exposed;

    public override void _Ready()
    {
        _enemies = GetNode<EnemySystem>("../EntitySystem");
        _elements = GetNode<ElementSystem>("../ElementSystem");
        _run = GetNode<RunController>("../RunController");
        _player = GetNode<CharacterBody3D>("../../WorldRoot/Player");
        _playerHealth = GetNode<PlayerHealth>("../../WorldRoot/Player/HealthComponent");
        _guardDefinition = ResourceLoader.Load<EnemyDefinition>(
            "res://resources/enemies/element_guard.tres");
        _enemies.EnemyRemoved += OnEnemyRemoved;
        _elements.ReactionResolved += OnReactionResolved;
    }

    public override void _ExitTree()
    {
        if (_enemies is not null)
        {
            _enemies.EnemyRemoved -= OnEnemyRemoved;
        }
        if (_elements is not null)
        {
            _elements.ReactionResolved -= OnReactionResolved;
        }
    }

    public override void _PhysicsProcess(double deltaValue)
    {
        if (_run.State != RunState.Playing)
        {
            return;
        }
        if (!HasSpawned && _run.ElapsedSeconds >= SpawnTime)
        {
            SpawnBoss();
        }
        if (!_boss.IsValid || !_enemies.TryGet(_boss, out EnemyState boss))
        {
            return;
        }

        float delta = (float)deltaValue;
        _actionRemaining = Math.Max(0.0f, _actionRemaining - delta);
        _nextSkillRemaining = Math.Max(0.0f, _nextSkillRemaining - delta);
        _contactRemaining = Math.Max(0.0f, _contactRemaining - delta);
        Vector2 playerPosition = new(_player.GlobalPosition.X, _player.GlobalPosition.Z);
        Vector2 offset = playerPosition - boss.Position;
        float distance = offset.Length();
        Vector2 direction = distance > 0.001f ? offset / distance : Vector2.Right;
        float frostMultiplier = Math.Max(
            0.45f,
            1.0f - boss.Elements.Frost.Stacks * 0.08f);

        if (boss.Elements.FrozenRemaining > 0.0f)
        {
            ElementRuntimeState runtime = boss.Elements;
            runtime.FrozenRemaining = 0.0f;
            _enemies.SetElementRuntime(_boss, runtime);
            AddStagger(12.0f);
        }

        if (_action != BossAction.Exposed && _stagger >= StaggerThreshold)
        {
            EnterExposed();
        }

        switch (_action)
        {
            case BossAction.Approach:
                if (distance > 5.5f)
                {
                    MoveBoss(boss.Position + direction * boss.MoveSpeed * frostMultiplier * delta);
                }
                if (_nextSkillRemaining <= 0.0f)
                {
                    StartNextSkill(boss.Position, direction);
                }
                break;
            case BossAction.SlamTelegraph:
                if (_actionRemaining <= 0.0f)
                {
                    ResolveSlam(boss.Position, playerPosition);
                    _action = BossAction.Recovery;
                    _actionRemaining = 0.8f;
                }
                break;
            case BossAction.ChargeTelegraph:
                if (_actionRemaining <= 0.0f)
                {
                    _action = BossAction.Charging;
                    _actionRemaining = 0.72f;
                    _contactRemaining = 0.0f;
                }
                break;
            case BossAction.Charging:
                MoveBoss(boss.Position + _lockedDirection * 12.0f * frostMultiplier * delta);
                if (distance <= 2.6f && _contactRemaining <= 0.0f)
                {
                    _playerHealth.ApplyDamage(18.0f, "灰烬巨像冲锋");
                    _contactRemaining = 0.8f;
                }
                if (_actionRemaining <= 0.0f)
                {
                    _chargesRemaining--;
                    if (_chargesRemaining > 0)
                    {
                        StartChargeTelegraph(boss.Position, direction, 0.4f);
                    }
                    else
                    {
                        _action = BossAction.ChargeEndTelegraph;
                        _actionRemaining = 0.7f;
                        _enemies.TelegraphAttack(
                            boss.Position, 4.2f, EnemyArchetype.BossAshenColossus);
                    }
                }
                break;
            case BossAction.ChargeEndTelegraph:
                if (_actionRemaining <= 0.0f)
                {
                    if (distance <= 4.2f)
                    {
                        _playerHealth.ApplyDamage(24.0f, "灰烬巨像余烬爆发");
                    }
                    EnterApproach(1.1f);
                }
                break;
            case BossAction.SummonTelegraph:
                if (_actionRemaining <= 0.0f)
                {
                    SummonGuards(boss.Position);
                    EnterApproach(1.5f);
                }
                break;
            case BossAction.Recovery:
                if (_actionRemaining <= 0.0f)
                {
                    EnterApproach(1.0f);
                }
                break;
            case BossAction.Exposed:
                if (_actionRemaining <= 0.0f)
                {
                    _enemies.SetDamageTakenMultiplier(_boss, 1.0f);
                    EnterApproach(1.2f);
                }
                break;
        }
    }

    public bool SpawnBoss()
    {
        if (HasSpawned || Definition is null)
        {
            return false;
        }
        Vector2 playerPosition = new(_player.GlobalPosition.X, _player.GlobalPosition.Z);
        _boss = _enemies.Spawn(playerPosition + new Vector2(0.0f, -18.0f), Definition);
        if (!_boss.IsValid)
        {
            return false;
        }
        HasSpawned = true;
        EnterApproach(2.0f);
        BossSpawned?.Invoke(_boss);
        CatalystLog.Info("Boss", "Ashen Colossus entered the arena.");
        return true;
    }

    public void AddStagger(float amount)
    {
        if (!_boss.IsValid || _action == BossAction.Exposed || amount <= 0.0f)
        {
            return;
        }
        _stagger = Math.Min(StaggerThreshold, _stagger + amount);
        StaggerChanged?.Invoke(_stagger, StaggerThreshold);
    }

    private void StartNextSkill(Vector2 position, Vector2 direction)
    {
        switch (_skillIndex++ % 3)
        {
            case 0:
                _action = BossAction.SlamTelegraph;
                _actionRemaining = 1.1f;
                _lockedDirection = direction;
                _enemies.TelegraphAttack(
                    position, 8.0f, EnemyArchetype.BossAshenColossus);
                break;
            case 1:
                _chargesRemaining = 3;
                StartChargeTelegraph(position, direction, 0.8f);
                break;
            default:
                _action = BossAction.SummonTelegraph;
                _actionRemaining = 1.2f;
                _enemies.TelegraphAttack(
                    position, 5.0f, EnemyArchetype.BossAshenColossus);
                break;
        }
    }

    private void StartChargeTelegraph(Vector2 position, Vector2 direction, float duration)
    {
        _action = BossAction.ChargeTelegraph;
        _actionRemaining = duration;
        _lockedDirection = direction;
        _enemies.TelegraphAttack(
            position, 9.0f, EnemyArchetype.BossAshenColossus);
    }

    private void ResolveSlam(Vector2 bossPosition, Vector2 playerPosition)
    {
        Vector2 offset = playerPosition - bossPosition;
        float distance = offset.Length();
        Vector2 direction = distance > 0.001f ? offset / distance : _lockedDirection;
        if (distance <= 8.0f && direction.Dot(_lockedDirection) >= 0.707f)
        {
            _playerHealth.ApplyDamage(28.0f, "灰烬巨像锥形重击");
        }
    }

    private void SummonGuards(Vector2 bossPosition)
    {
        for (int index = 0; index < 3; index++)
        {
            Vector2 offset = Vector2.FromAngle(Mathf.Tau * index / 3.0f) * 4.0f;
            _enemies.Spawn(bossPosition + offset, _guardDefinition);
        }
    }

    private void EnterExposed()
    {
        _action = BossAction.Exposed;
        _actionRemaining = 4.0f;
        _stagger = 0.0f;
        _enemies.SetDamageTakenMultiplier(_boss, 1.6f);
        StaggerChanged?.Invoke(_stagger, StaggerThreshold);
        CatalystLog.Info("Boss", "Ashen Colossus staggered: exposed weakness active.");
    }

    private void EnterApproach(float delay)
    {
        _action = BossAction.Approach;
        _nextSkillRemaining = delay;
    }

    private void MoveBoss(Vector2 position)
    {
        _enemies.SetPosition(_boss, position);
    }

    private void OnReactionResolved(EntityHandle target, ReactionKind kind, float damage)
    {
        if (target != _boss)
        {
            return;
        }
        AddStagger(kind switch
        {
            ReactionKind.SteamShock => 36.0f,
            ReactionKind.Conduction => 34.0f,
            _ => 0.0f
        });
    }

    private void OnEnemyRemoved(EnemyState enemy, bool grantedReward)
    {
        if (enemy.Handle != _boss)
        {
            return;
        }
        _boss = EntityHandle.Invalid;
        IsDefeated = true;
        BossDefeated?.Invoke();
        CatalystLog.Info("Boss", "Ashen Colossus defeated.");
        _run.EnterVictory();
    }

    private enum BossAction : byte
    {
        Approach,
        SlamTelegraph,
        ChargeTelegraph,
        Charging,
        ChargeEndTelegraph,
        SummonTelegraph,
        Recovery,
        Exposed
    }
}
