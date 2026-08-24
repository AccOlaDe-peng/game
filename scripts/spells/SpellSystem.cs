using Catalyst.Combat;
using Catalyst.Enemies;
using Catalyst.Elements;
using Catalyst.App;
using Catalyst.Projectiles;
using Catalyst.Run;
using Catalyst.Meta;
using Catalyst.Weapons;
using Catalyst.Cards;
using Catalyst.Spatial;
using Catalyst.Player;
using Godot;

namespace Catalyst.Spells;

public partial class SpellSystem : Node
{
    public event Action<Vector2, Vector2>? LightningJumped;
    public event Action? LoadoutChanged;
    public event Action<SpellCastKind>? SpellCast;

    [Export] public float TargetRange { get; set; } = 32.0f;
    [Export] public bool Enabled { get; set; } = true;

    public float DamageMultiplier { get; set; } = 1.0f;
    public float CooldownMultiplier { get; set; } = 1.0f;
    public float ProjectileSpeedMultiplier { get; set; } = 1.0f;
    public IReadOnlyList<SpellRuntime> Loadout => _loadout;
    public int SlotCapacity => 4;

    private readonly Dictionary<SpellCastKind, SpellDefinition> _definitions = new();
    private readonly List<SpellRuntime> _loadout = new(4);
    private readonly List<EntityHandle> _queryCandidates = new(64);
    private readonly List<EntityHandle> _chainHits = new(9);
    private CharacterBody3D _player = null!;
    private EnemySystem _enemies = null!;
    private SpatialGrid _grid = null!;
    private ProjectileSystem _projectiles = null!;
    private CombatSystem _combat = null!;
    private RunController _run = null!;
    private PlayerController _playerController = null!;
    private ElementSystem _elements = null!;
    private readonly List<WeaponFieldState> _fields = new(16);

    public override void _Ready()
    {
        ContentCatalog catalog = GetNode<ContentCatalog>("/root/ContentCatalog");
        foreach (SpellDefinition definition in catalog.All<SpellDefinition>())
        {
            _definitions[definition.CastKind] = definition;
        }
        if (_definitions.Count != 7)
        {
            GD.PushError($"Expected 7 spell definitions, loaded {_definitions.Count}.");
        }

        _player = GetNode<CharacterBody3D>("../../WorldRoot/Player");
        _enemies = GetNode<EnemySystem>("../EntitySystem");
        _grid = GetNode<SpatialGrid>("../SpatialGrid");
        _projectiles = GetNode<ProjectileSystem>("../ProjectileSystem");
        _combat = GetNode<CombatSystem>("../CombatSystem");
        _run = GetNode<RunController>("../RunController");
        _playerController = GetNode<PlayerController>("../../WorldRoot/Player");
        _elements = GetNode<ElementSystem>("../ElementSystem");
        _combat.HitResolved += OnHitResolved;
        _elements.Frozen += OnFrozen;
        _playerController.Dodged += OnDodged;
        _projectiles.FieldRequested += OnFieldRequested;
        CharacterDefinition character = GetNode<SaveService>("/root/SaveService").ActiveCharacter;
        AcquireSpell(character.PrototypeStartingSpell);
    }

    public override void _PhysicsProcess(double deltaValue)
    {
        if (_run.State != RunState.Playing || !Enabled)
        {
            return;
        }

        float delta = (float)deltaValue;
        Vector2 playerPosition = new(_player.GlobalPosition.X, _player.GlobalPosition.Z);
        EnemyState nearestEnemy = default;
        bool hasTarget = _grid.TryFindNearest(
            playerPosition, TargetRange, out EntityHandle nearestTarget) &&
            _enemies.TryGet(nearestTarget, out nearestEnemy);

        foreach (SpellRuntime runtime in _loadout)
        {
            runtime.CooldownRemaining -= delta;
            if (runtime.CooldownRemaining > 0.0f)
            {
                continue;
            }

            bool cast = runtime.Definition.CastKind switch
            {
                SpellCastKind.ArcaneMissile when hasTarget =>
                    CastProjectileFan(runtime, playerPosition, nearestEnemy.Position),
                SpellCastKind.Fireball when hasTarget =>
                    CastProjectileFan(runtime, playerPosition, nearestEnemy.Position),
                SpellCastKind.FrostLance when hasTarget =>
                    CastProjectileFan(runtime, playerPosition, nearestEnemy.Position),
                SpellCastKind.ChainLightning when hasTarget =>
                    CastChainLightning(runtime, nearestTarget),
                SpellCastKind.OrbitOrb => CastOrbitOrb(runtime, playerPosition),
                SpellCastKind.RicochetDisc when hasTarget =>
                    CastRicochetDisc(runtime, playerPosition, nearestEnemy.Position),
                SpellCastKind.ElementMine when hasTarget =>
                    CastElementMine(runtime, nearestEnemy.Position),
                _ => false
            };

            if (cast)
            {
                SpellCast?.Invoke(runtime.Definition.CastKind);
                runtime.CooldownRemaining = Math.Max(
                    0.05f,
                    runtime.Stats.Cooldown * CooldownMultiplier);
            }
        }
        UpdateFields(delta);
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_combat)) _combat.HitResolved -= OnHitResolved;
        if (IsInstanceValid(_elements)) _elements.Frozen -= OnFrozen;
        if (IsInstanceValid(_playerController)) _playerController.Dodged -= OnDodged;
        if (IsInstanceValid(_projectiles)) _projectiles.FieldRequested -= OnFieldRequested;
    }

    public bool CanAcquireSpell(SpellCastKind kind) =>
        _loadout.Count < SlotCapacity && !_loadout.Any(runtime => runtime.Definition.CastKind == kind);

    public bool AcquireSpell(SpellCastKind kind)
    {
        if (!CanAcquireSpell(kind) || !_definitions.TryGetValue(kind, out SpellDefinition? definition))
        {
            return false;
        }
        _loadout.Add(new SpellRuntime(definition));
        LoadoutChanged?.Invoke();
        return true;
    }

    public bool UpgradeSpell(SpellCastKind kind, SpellBranch branch)
    {
        if (!TryGetRuntime(kind, out SpellRuntime? runtime) || runtime is null ||
            !runtime.Upgrade(branch))
        {
            return false;
        }
        LoadoutChanged?.Invoke();
        return true;
    }

    public bool InstallWeaponCard(SpellCastKind kind, WeaponCardDefinition card)
    {
        if (!TryGetRuntime(kind, out SpellRuntime? runtime) || runtime is null ||
            !runtime.InstallCard(card))
        {
            return false;
        }
        LoadoutChanged?.Invoke();
        return true;
    }

    public bool InstallStandardCard(SpellCastKind kind, StandardCardDefinition card)
    {
        if (!TryGetRuntime(kind, out SpellRuntime? runtime) || runtime is null ||
            !runtime.InstallStandardCard(card))
        {
            return false;
        }
        LoadoutChanged?.Invoke();
        return true;
    }

    public bool TryGetRuntime(SpellCastKind kind, out SpellRuntime? runtime)
    {
        runtime = _loadout.FirstOrDefault(item => item.Definition.CastKind == kind);
        return runtime is not null;
    }

    public SpellDefinition GetDefinition(SpellCastKind kind) => _definitions[kind];

    public void UnlockElementSpellsForTests()
    {
        AcquireSpell(SpellCastKind.Fireball);
        AcquireSpell(SpellCastKind.FrostLance);
        AcquireSpell(SpellCastKind.ChainLightning);
    }

    private bool CastProjectileFan(SpellRuntime runtime, Vector2 origin, Vector2 target)
    {
        SpellStats stats = runtime.Stats;
        Vector2 direction = target - origin;
        if (direction.LengthSquared() < 0.0001f)
        {
            return false;
        }

        bool cast = false;
        float startDegrees = -stats.SpreadDegrees * (stats.Count - 1) * 0.5f;
        for (int index = 0; index < stats.Count; index++)
        {
            float angle = Mathf.DegToRad(startDegrees + index * stats.SpreadDegrees);
            cast |= _projectiles.Spawn(
                origin,
                direction.Rotated(angle),
                stats.Damage * DamageMultiplier,
                stats.ProjectileSpeed * ProjectileSpeedMultiplier,
                stats.Lifetime,
                runtime.Definition.Id,
                stats.Element,
                stats.ElementStacks,
                stats.MaximumHits,
                GetDirectExplosionRadius(runtime, stats.ExplosionRadius),
                modules: BuildProjectileModules(runtime),
                secondaryElement: stats.SecondaryElement,
                fusion: stats.Fusion);
        }
        return cast;
    }

    private bool CastRicochetDisc(SpellRuntime runtime, Vector2 origin, Vector2 target)
    {
        SpellStats stats = runtime.Stats;
        Vector2 direction = target - origin;
        if (direction.LengthSquared() < 0.0001f)
        {
            return false;
        }
        bool cast = false;
        float spread = stats.Count > 1 ? 10.0f : 0.0f;
        float startDegrees = -spread * (stats.Count - 1) * 0.5f;
        for (int index = 0; index < stats.Count; index++)
        {
            Vector2 discDirection = direction.Rotated(Mathf.DegToRad(startDegrees + index * spread));
            cast |= _projectiles.Spawn(origin, discDirection, stats.Damage * DamageMultiplier,
                stats.ProjectileSpeed * ProjectileSpeedMultiplier, stats.Lifetime,
                runtime.Definition.Id, stats.Element, stats.ElementStacks,
                Math.Max(3, stats.MaximumHits), 0.0f,
                Math.Max(2, stats.ChainCount), Math.Max(4.0f, stats.ChainRange),
                modules: BuildProjectileModules(runtime),
                secondaryElement: stats.SecondaryElement,
                fusion: stats.Fusion);
        }
        return cast;
    }

    private bool CastElementMine(SpellRuntime runtime, Vector2 target)
    {
        SpellStats stats = runtime.Stats;
        return _projectiles.SpawnMine(target, stats.Damage * DamageMultiplier,
            Math.Max(0.1f, stats.Lifetime), Math.Max(1.0f, stats.ExplosionRadius),
            runtime.Definition.Id, stats.Element, stats.ElementStacks,
            BuildProjectileModules(runtime), stats.SecondaryElement, stats.Fusion);
    }

    private static ProjectileModules BuildProjectileModules(SpellRuntime runtime)
    {
        ProjectileModules modules = ProjectileModules.None;
        if (runtime.HasStandardEffect(StandardCardEffect.Split)) modules |= ProjectileModules.Split;
        if (runtime.HasStandardEffect(StandardCardEffect.Homing)) modules |= ProjectileModules.Homing;
        if (runtime.HasStandardEffect(StandardCardEffect.Boomerang)) modules |= ProjectileModules.Boomerang;
        if (runtime.HasStandardEffect(StandardCardEffect.Fragments)) modules |= ProjectileModules.Fragments;
        if (runtime.HasStandardEffect(StandardCardEffect.Nova)) modules |= ProjectileModules.Nova;
        if (runtime.HasStandardEffect(StandardCardEffect.GroundField)) modules |= ProjectileModules.GroundField;
        if (runtime.HasStandardEffect(StandardCardEffect.StatusSpread)) modules |= ProjectileModules.StatusSpread;
        if (runtime.HasStandardEffect(StandardCardEffect.Explosion)) modules |= ProjectileModules.ExplosionAction;
        if (runtime.HasStandardEffect(StandardCardEffect.OnPierce)) modules |= ProjectileModules.TriggerOnPierce;
        if (runtime.HasStandardEffect(StandardCardEffect.OnBounce)) modules |= ProjectileModules.TriggerOnBounce;
        return modules;
    }

    private static float GetDirectExplosionRadius(SpellRuntime runtime, float radius)
    {
        bool hasTrigger = runtime.HasStandardEffect(StandardCardEffect.OnPierce) ||
            runtime.HasStandardEffect(StandardCardEffect.OnBounce) ||
            runtime.HasStandardEffect(StandardCardEffect.OnFreeze) ||
            runtime.HasStandardEffect(StandardCardEffect.OnDash);
        return hasTrigger ? 0.0f : radius;
    }

    private SpellRuntime? FindRuntime(StringName sourceId) =>
        _loadout.FirstOrDefault(runtime => runtime.Definition.Id == sourceId);

    private void OnFrozen(EntityHandle target, DamageContext context)
    {
        SpellRuntime? runtime = FindRuntime(context.SourceSpellId);
        if (runtime is null || !runtime.HasStandardEffect(StandardCardEffect.OnFreeze) ||
            !runtime.HasStandardEffect(StandardCardEffect.Fragments) ||
            !_enemies.TryGet(target, out EnemyState enemy)) return;
        _projectiles.SpawnRadial(enemy.Position, 4, runtime.Stats.Damage * 0.70f,
            runtime.Stats.ProjectileSpeed * 0.85f, runtime.Definition.Id,
            runtime.Stats.Element, runtime.Stats.ElementStacks, context.ChainDepth + 1);
    }

    private void OnDodged()
    {
        Vector2 center = new(_player.GlobalPosition.X, _player.GlobalPosition.Z);
        foreach (SpellRuntime runtime in _loadout)
        {
            if (runtime.HasStandardEffect(StandardCardEffect.OnDash) &&
                runtime.HasStandardEffect(StandardCardEffect.GroundField))
            {
                AddField(center, runtime.Definition.Id, runtime.Stats.Damage,
                    runtime.Stats.Element, runtime.Stats.ElementStacks, 0);
            }
        }
    }

    private void OnFieldRequested(Vector2 position, StringName sourceId, float damage,
        ElementType element, int stacks, int chainDepth) =>
        AddField(position, sourceId, damage, element, stacks, chainDepth);

    private void OnHitResolved(DamageContext context, EnemyState beforeHit, bool killed)
    {
        if (context.Flags.HasFlag(DamageFlags.IsReactionDamage) || context.ChainDepth >= 4) return;
        SpellRuntime? runtime = FindRuntime(context.SourceSpellId);
        if (runtime is null) return;

        if (killed)
        {
            if (runtime.HasStandardEffect(StandardCardEffect.Shatter) &&
                beforeHit.Elements.FrozenRemaining > 0.0f)
            {
                _projectiles.SpawnRadial(beforeHit.Position, 5, runtime.Stats.Damage * 0.70f,
                    runtime.Stats.ProjectileSpeed * 0.85f, runtime.Definition.Id,
                    ElementType.Frost, 1, context.ChainDepth + 1);
            }
            if (runtime.HasStandardEffect(StandardCardEffect.BurnPropagation) &&
                beforeHit.Elements.Fire.Stacks > 0)
            {
                SubmitArea(beforeHit.Position, 3.5f, runtime, 0.0f, ElementType.Fire,
                    context.ChainDepth + 1);
            }
            if (runtime.HasStandardEffect(StandardCardEffect.DeathNova))
            {
                SubmitArea(beforeHit.Position, 3.2f, runtime, 0.75f,
                    runtime.Stats.Element, context.ChainDepth + 1);
            }
            if (runtime.HasStandardEffect(StandardCardEffect.StatusSpread))
            {
                SubmitArea(beforeHit.Position, 3.5f, runtime, 0.0f,
                    runtime.Stats.Element, context.ChainDepth + 1);
            }
            if (runtime.HasStandardEffect(StandardCardEffect.GroundField) &&
                runtime.Stats.Element == ElementType.Fire)
            {
                AddField(beforeHit.Position, runtime.Definition.Id, runtime.Stats.Damage,
                    runtime.Stats.Element, runtime.Stats.ElementStacks, context.ChainDepth + 1);
            }
        }
        else if (runtime.HasStandardEffect(StandardCardEffect.CriticalEcho) &&
            _run.RandomStreams.GameplayProc.Randf() <= 0.20f)
        {
            Vector2 origin = new(_player.GlobalPosition.X, _player.GlobalPosition.Z);
            _projectiles.Spawn(origin, beforeHit.Position - origin, runtime.Stats.Damage * 0.50f,
                runtime.Stats.ProjectileSpeed, 1.2f, runtime.Definition.Id,
                runtime.Stats.Element, runtime.Stats.ElementStacks, 1,
                chainDepth: context.ChainDepth + 1,
                secondaryElement: runtime.Stats.SecondaryElement, fusion: runtime.Stats.Fusion);
        }
    }

    private void AddField(Vector2 position, StringName sourceId, float damage,
        ElementType element, int stacks, int chainDepth)
    {
        if (_fields.Count >= WeaponFieldRules.MaximumFields || chainDepth > 4) return;
        _fields.Add(new WeaponFieldState
        {
            Position = position,
            SourceId = sourceId,
            Damage = damage,
            Element = element,
            ElementStacks = stacks,
            Remaining = WeaponFieldRules.Duration,
            PulseRemaining = 0.0f,
            ChainDepth = chainDepth
        });
    }

    private void UpdateFields(float delta)
    {
        for (int index = _fields.Count - 1; index >= 0; index--)
        {
            WeaponFieldState field = _fields[index];
            field.Remaining -= delta;
            field.PulseRemaining -= delta;
            if (field.PulseRemaining <= 0.0f)
            {
                field.PulseRemaining += WeaponFieldRules.PulseInterval;
                _grid.QueryCircle(field.Position, 3.0f, _queryCandidates);
                foreach (EntityHandle target in _queryCandidates.Take(8))
                {
                    _combat.Submit(new DamageContext(target, field.SourceId,
                        field.Damage * 0.28f, field.Element, field.ElementStacks,
                        DamageFlags.CanTriggerReaction, field.ChainDepth + 1));
                }
            }
            if (field.Remaining <= 0.0f) _fields.RemoveAt(index);
            else _fields[index] = field;
        }
    }

    private void SubmitArea(Vector2 center, float radius, SpellRuntime runtime,
        float damageScale, ElementType element, int chainDepth)
    {
        _grid.QueryCircle(center, radius, _queryCandidates);
        foreach (EntityHandle target in _queryCandidates.Take(8))
        {
            _combat.Submit(new DamageContext(target, runtime.Definition.Id,
                runtime.Stats.Damage * damageScale, element, runtime.Stats.ElementStacks,
                DamageFlags.CanTriggerReaction, chainDepth));
        }
    }

    private bool CastChainLightning(SpellRuntime runtime, EntityHandle firstTarget)
    {
        SpellStats stats = runtime.Stats;
        _chainHits.Clear();
        EntityHandle current = firstTarget;
        Vector2 previousPosition = new(_player.GlobalPosition.X, _player.GlobalPosition.Z);
        for (int jump = 0; jump < stats.ChainCount; jump++)
        {
            if (!_enemies.TryGet(current, out EnemyState currentEnemy))
            {
                break;
            }

            _chainHits.Add(current);
            LightningJumped?.Invoke(previousPosition, currentEnemy.Position);
            previousPosition = currentEnemy.Position;
            _combat.Submit(new DamageContext(
                current,
                runtime.Definition.Id,
                stats.Damage * DamageMultiplier,
                stats.Element,
                stats.ElementStacks,
                DamageFlags.CanTriggerReaction,
                SecondaryElement: stats.SecondaryElement,
                Fusion: stats.Fusion));

            _grid.QueryCircle(currentEnemy.Position, stats.ChainRange, _queryCandidates);
            EntityHandle next = EntityHandle.Invalid;
            float bestDistance = float.MaxValue;
            foreach (EntityHandle candidate in _queryCandidates)
            {
                if (_chainHits.Contains(candidate) ||
                    !_enemies.TryGet(candidate, out EnemyState candidateEnemy))
                {
                    continue;
                }
                float distance = currentEnemy.Position.DistanceSquaredTo(candidateEnemy.Position);
                if (distance < bestDistance ||
                    (Mathf.IsEqualApprox(distance, bestDistance) && candidate.Index < next.Index))
                {
                    bestDistance = distance;
                    next = candidate;
                }
            }
            if (!next.IsValid)
            {
                break;
            }
            current = next;
        }
        return _chainHits.Count > 0;
    }

    private bool CastOrbitOrb(SpellRuntime runtime, Vector2 playerPosition)
    {
        SpellStats stats = runtime.Stats;
        _grid.QueryCircle(playerPosition, stats.OrbitRadius, _queryCandidates);
        if (_queryCandidates.Count == 0)
        {
            return false;
        }

        _queryCandidates.Sort((left, right) =>
        {
            float leftDistance = _enemies.TryGet(left, out EnemyState leftEnemy)
                ? playerPosition.DistanceSquaredTo(leftEnemy.Position)
                : float.MaxValue;
            float rightDistance = _enemies.TryGet(right, out EnemyState rightEnemy)
                ? playerPosition.DistanceSquaredTo(rightEnemy.Position)
                : float.MaxValue;
            int order = leftDistance.CompareTo(rightDistance);
            return order != 0 ? order : left.Index.CompareTo(right.Index);
        });

        int hitCount = Math.Min(stats.Count, _queryCandidates.Count);
        for (int index = 0; index < hitCount; index++)
        {
            _combat.Submit(new DamageContext(
                _queryCandidates[index],
                runtime.Definition.Id,
                stats.Damage * DamageMultiplier,
                stats.Element,
                stats.ElementStacks,
                stats.Element == ElementType.None
                    ? DamageFlags.None
                    : DamageFlags.CanTriggerReaction,
                SecondaryElement: stats.SecondaryElement,
                Fusion: stats.Fusion));
        }
        return true;
    }
}
