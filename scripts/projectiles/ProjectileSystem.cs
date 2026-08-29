using Catalyst.Combat;
using Catalyst.Core;
using Catalyst.Elements;
using Catalyst.Enemies;
using Catalyst.Presentation;
using Catalyst.Run;
using Catalyst.Spatial;
using Godot;

namespace Catalyst.Projectiles;

public partial class ProjectileSystem : Node
{
    public event Action<Vector2, StringName, float, ElementType, int, int>? FieldRequested;
    [Export(PropertyHint.Range, "1,5000,1")]
    public int Capacity { get; set; } = 1024;

    private ProjectileState[] _projectiles = Array.Empty<ProjectileState>();
    private readonly List<EntityHandle> _candidates = new(64);
    private readonly List<EntityHandle> _splashCandidates = new(64);
    private EnemySystem _enemies = null!;
    private SpatialGrid _grid = null!;
    private RunController _run = null!;
    private CombatSystem _combat = null!;
    private CharacterBody3D _player = null!;
    private MultiMeshInstance3D _view = null!;
    private MultiMesh _multiMesh = null!;

    private static readonly StringName RicochetDiscId = new("weapon.ricochet_disc");

    public int ActiveCount { get; private set; }

    /// <summary>Rift engineer capacity safety: trigger the oldest deployment when full.</summary>
    public bool CapacitySafetyEnabled { get; set; }

    /// <summary>Echo hunter mark priority: bounce/homing prefers marked targets.</summary>
    public bool PreferMarkedTargets { get; set; }

    /// <summary>Echo hunter auto recall: discs return once per projectile.</summary>
    public bool AutoRecallEnabled { get; set; }

    [Export] public int DeploymentLimit { get; set; } = 6;
    [Export] public float AutoRecallTurnRateRadiansPerSecond { get; set; } = 7.0f;

    public float NextMineDamageMultiplier { get; set; } = 1.0f;
    public float NextMineRadiusMultiplier { get; set; } = 1.0f;

    public int ActiveMineCount
    {
        get
        {
            int count = 0;
            for (int index = 0; index < ActiveCount; index++)
            {
                if (_projectiles[index].DetonateOnExpire)
                {
                    count++;
                }
            }
            return count;
        }
    }

    public override void _Ready()
    {
        _enemies = GetNode<EnemySystem>("../EntitySystem");
        _grid = GetNode<SpatialGrid>("../SpatialGrid");
        _run = GetNode<RunController>("../RunController");
        _combat = GetNode<CombatSystem>("../CombatSystem");
        _player = GetNode<CharacterBody3D>("../../WorldRoot/Player");
        _view = GetNode<MultiMeshInstance3D>("../../WorldRoot/ProjectilePresentation/ArcaneMultiMesh");
        _projectiles = new ProjectileState[Capacity];
        BuildPresentation();
    }

    public override void _PhysicsProcess(double deltaValue)
    {
        if (_run.State != RunState.Playing)
        {
            return;
        }

        float delta = (float)deltaValue;
        int index = 0;
        while (index < ActiveCount)
        {
            ref ProjectileState projectile = ref _projectiles[index];
            ApplyHoming(ref projectile, delta);
            ApplyAutoRecall(ref projectile, delta);
            projectile.PreviousPosition = projectile.Position;
            projectile.Position += projectile.Velocity * delta;
            projectile.RemainingLife -= delta;

            bool expired = projectile.RemainingLife <= 0.0f;
            if (expired && projectile.DetonateOnExpire)
            {
                SubmitExplosionAt(projectile, projectile.Position);
            }
            bool consumed = expired || TryResolveHit(ref projectile);
            if (consumed)
            {
                RemoveAt(index);
                continue;
            }

            index++;
        }

        SyncPresentation();
    }

    public bool Spawn(
        Vector2 position,
        Vector2 direction,
        float damage,
        float speed,
        float lifetime,
        StringName sourceSpellId,
        ElementType element = ElementType.None,
        int elementStacks = 0,
        int maximumHits = 1,
        float explosionRadius = 0.0f,
        int bounceCount = 0,
        float bounceRange = 0.0f,
        bool detonateOnExpire = false,
        ProjectileModules modules = ProjectileModules.None,
        int chainDepth = 0,
        int childEventBudget = ProjectileEffectBudget.MaximumChildEvents,
        ElementType secondaryElement = ElementType.None,
        FusionIdentity fusion = FusionIdentity.None)
    {
        if (ActiveCount >= Capacity || direction.LengthSquared() < 0.0001f)
        {
            if (ActiveCount >= Capacity)
            {
                CatalystLog.Warning("Projectiles", $"Projectile capacity exhausted at {ActiveCount}/{Capacity}.");
            }
            return false;
        }

        direction = direction.Normalized();
        _projectiles[ActiveCount++] = new ProjectileState
        {
            Position = position,
            PreviousPosition = position,
            Velocity = direction * speed,
            Radius = 0.22f,
            RemainingLife = lifetime,
            InitialLife = lifetime,
            Damage = damage,
            SourceSpellId = sourceSpellId,
            Element = element,
            SecondaryElement = secondaryElement,
            Fusion = fusion,
            ElementStacks = elementStacks,
            Flags = element == ElementType.None
                ? DamageFlags.None
                : DamageFlags.CanTriggerReaction,
            ExplosionRadius = explosionRadius,
            RemainingHits = Mathf.Clamp(maximumHits, 1, 8),
            RemainingBounces = Math.Max(0, bounceCount),
            BounceRange = Math.Max(0.0f, bounceRange),
            DetonateOnExpire = detonateOnExpire,
            Modules = modules,
            Origin = position,
            ChainDepth = chainDepth,
            ChildEventsRemaining = Math.Max(0, childEventBudget),
            Hit0 = EntityHandle.Invalid,
            Hit1 = EntityHandle.Invalid,
            Hit2 = EntityHandle.Invalid,
            Hit3 = EntityHandle.Invalid,
            Hit4 = EntityHandle.Invalid,
            Hit5 = EntityHandle.Invalid,
            Hit6 = EntityHandle.Invalid,
            Hit7 = EntityHandle.Invalid
        };
        return true;
    }

    public bool SpawnMine(
        Vector2 position,
        float damage,
        float fuseSeconds,
        float explosionRadius,
        StringName sourceSpellId,
        ElementType element,
        int elementStacks,
        ProjectileModules modules = ProjectileModules.None,
        ElementType secondaryElement = ElementType.None,
        FusionIdentity fusion = FusionIdentity.None)
    {
        float damageMultiplier = NextMineDamageMultiplier;
        float radiusMultiplier = NextMineRadiusMultiplier;
        NextMineDamageMultiplier = 1.0f;
        NextMineRadiusMultiplier = 1.0f;
        return Spawn(position, Vector2.Right, damage * damageMultiplier, 0.0f, fuseSeconds, sourceSpellId,
            element, elementStacks, 1, explosionRadius * radiusMultiplier, 0, 0.0f, true, modules,
            secondaryElement: secondaryElement, fusion: fusion);
    }

    /// <summary>
    /// Capacity gate for new deployments. With capacity safety enabled the
    /// oldest deployment is triggered first; without it creation is refused.
    /// </summary>
    public bool EnsureDeploymentCapacity()
    {
        if (ActiveMineCount < DeploymentLimit)
        {
            return true;
        }
        return CapacitySafetyEnabled && TriggerOldestMine();
    }

    public bool TriggerOldestMine()
    {
        for (int index = 0; index < ActiveCount; index++)
        {
            if (_projectiles[index].DetonateOnExpire)
            {
                return DetonateAt(index);
            }
        }
        return false;
    }

    public bool TriggerNearestMine(Vector2 position)
    {
        int bestIndex = -1;
        float bestDistance = float.MaxValue;
        for (int index = 0; index < ActiveCount; index++)
        {
            ref ProjectileState mine = ref _projectiles[index];
            if (!mine.DetonateOnExpire)
            {
                continue;
            }
            float distance = mine.Position.DistanceSquaredTo(position);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestIndex = index;
            }
        }
        return bestIndex >= 0 && DetonateAt(bestIndex);
    }

    /// <summary>Number of deployments whose blast radius reaches the position.</summary>
    public int CountMinesCovering(Vector2 position, float reach)
    {
        int count = 0;
        for (int index = 0; index < ActiveCount; index++)
        {
            ref ProjectileState mine = ref _projectiles[index];
            if (mine.DetonateOnExpire &&
                mine.Position.DistanceTo(position) <= Math.Max(reach, mine.ExplosionRadius))
            {
                count++;
            }
        }
        return count;
    }

    private bool DetonateAt(int index)
    {
        ProjectileState mine = _projectiles[index];
        SubmitExplosionAt(mine, mine.Position);
        RemoveAt(index);
        return true;
    }

    public void SpawnRadial(
        Vector2 center,
        int count,
        float damage,
        float speed,
        StringName sourceId,
        ElementType element,
        int elementStacks,
        int chainDepth)
    {
        int clamped = Mathf.Clamp(count, 2, 6);
        for (int index = 0; index < clamped; index++)
        {
            Spawn(center, Vector2.FromAngle(Mathf.Tau * index / clamped), damage, speed,
                0.9f, sourceId, element, elementStacks, 2, chainDepth: chainDepth);
        }
    }

    public void ClearAll()
    {
        Array.Clear(_projectiles, 0, ActiveCount);
        ActiveCount = 0;
        SyncPresentation();
    }

    private bool TryResolveHit(ref ProjectileState projectile)
    {
        if (projectile.DetonateOnExpire)
        {
            return false;
        }
        Vector2 midpoint = (projectile.PreviousPosition + projectile.Position) * 0.5f;
        float segmentLength = projectile.PreviousPosition.DistanceTo(projectile.Position);
        _grid.QueryCircle(midpoint, segmentLength * 0.5f + projectile.Radius + 1.2f, _candidates);

        foreach (EntityHandle candidate in _candidates)
        {
            if (HasHit(projectile, candidate) ||
                !_enemies.TryGet(candidate, out EnemyState enemy))
            {
                continue;
            }

            float hitRadius = projectile.Radius + enemy.Radius;
            if (DistanceSquaredToSegment(
                    enemy.Position,
                    projectile.PreviousPosition,
                    projectile.Position) > hitRadius * hitRadius)
            {
                continue;
            }

            RecordHit(ref projectile, candidate);
            if (projectile.Modules.HasFlag(ProjectileModules.Split) && !projectile.HasSplit)
            {
                projectile.HasSplit = true;
                SpawnFragments(projectile, enemy.Position, 2, 0.65f);
            }
            if (projectile.ExplosionRadius > 0.0f)
            {
                SubmitExplosion(projectile, enemy.Position, candidate);
                return true;
            }

            SubmitHit(projectile, candidate, projectile.Damage);
            projectile.RemainingHits--;
            if (projectile.RemainingBounces > 0 &&
                TryRedirectToBounceTarget(ref projectile, enemy.Position))
            {
                projectile.RemainingBounces--;
                if (projectile.Modules.HasFlag(ProjectileModules.TriggerOnBounce))
                {
                    ExecuteTriggeredActions(ref projectile, enemy.Position);
                }
                return false;
            }
            if (projectile.RemainingHits > 0 &&
                projectile.Modules.HasFlag(ProjectileModules.TriggerOnPierce))
            {
                ExecuteTriggeredActions(ref projectile, enemy.Position);
            }
            if (projectile.RemainingHits <= 0)
            {
                if (projectile.Modules.HasFlag(ProjectileModules.Boomerang) && !projectile.IsReturning)
                {
                    projectile.IsReturning = true;
                    projectile.RemainingHits = 1;
                    ClearHits(ref projectile);
                    Vector2 returnDirection = projectile.Origin - enemy.Position;
                    if (returnDirection.LengthSquared() > 0.0001f)
                    {
                        projectile.Velocity = returnDirection.Normalized() * projectile.Velocity.Length();
                        return false;
                    }
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Echo hunter auto recall: when a disc runs out of bounces or reaches 70%
    /// of its lifetime it turns back toward the player with one extra pierce.
    /// The return path continuously tracks the player but turns at a capped
    /// rate, and each projectile recalls at most once.
    /// </summary>
    private void ApplyAutoRecall(ref ProjectileState projectile, float delta)
    {
        if (!AutoRecallEnabled || projectile.DetonateOnExpire ||
            projectile.SourceSpellId != RicochetDiscId)
        {
            return;
        }

        if (!projectile.HasAutoRecalled &&
            (projectile.RemainingBounces <= 0 || projectile.RemainingLife <= projectile.InitialLife * 0.3f))
        {
            projectile.HasAutoRecalled = true;
            projectile.IsReturning = true;
            projectile.RemainingHits = Math.Max(projectile.RemainingHits, 0) + 1;
            ClearHits(ref projectile);
        }

        if (!projectile.HasAutoRecalled || projectile.Velocity.LengthSquared() < 0.0001f)
        {
            return;
        }

        Vector2 playerPosition = new(_player.GlobalPosition.X, _player.GlobalPosition.Z);
        float speed = projectile.Velocity.Length();
        float currentAngle = Mathf.Atan2(projectile.Velocity.Y, projectile.Velocity.X);
        Vector2 desired = playerPosition - projectile.Position;
        if (desired.LengthSquared() < 0.0001f)
        {
            return;
        }
        float targetAngle = Mathf.Atan2(desired.Y, desired.X);
        float maxTurn = AutoRecallTurnRateRadiansPerSecond * delta;
        float turn = Mathf.Wrap(targetAngle - currentAngle, -Mathf.Pi, Mathf.Pi);
        currentAngle += Mathf.Clamp(turn, -maxTurn, maxTurn);
        projectile.Velocity = new Vector2(Mathf.Cos(currentAngle), Mathf.Sin(currentAngle)) * speed;
    }

    private void ApplyHoming(ref ProjectileState projectile, float delta)
    {
        if (!projectile.Modules.HasFlag(ProjectileModules.Homing) || projectile.Velocity.LengthSquared() < 0.001f)
        {
            return;
        }
        _grid.QueryCircle(projectile.Position, 6.0f, _candidates);
        EntityHandle nearest = EntityHandle.Invalid;
        Vector2 targetPosition = default;
        float bestDistance = float.MaxValue;
        foreach (EntityHandle candidate in _candidates)
        {
            if (HasHit(projectile, candidate) || !_enemies.TryGet(candidate, out EnemyState enemy)) continue;
            float distance = projectile.Position.DistanceSquaredTo(enemy.Position);
            if (PreferMarkedTargets && enemy.Elements.Mark.Stacks > 0)
            {
                distance -= 1000.0f;
            }
            if (distance < bestDistance)
            {
                nearest = candidate;
                targetPosition = enemy.Position;
                bestDistance = distance;
            }
        }
        if (!nearest.IsValid) return;
        float speed = projectile.Velocity.Length();
        Vector2 desired = (targetPosition - projectile.Position).Normalized() * speed;
        projectile.Velocity = projectile.Velocity.Lerp(desired, 1.0f - MathF.Exp(-5.0f * delta));
    }

    private void ExecuteTriggeredActions(ref ProjectileState projectile, Vector2 center)
    {
        if (!ProjectileEffectBudget.CanSpawnChild(projectile.ChainDepth, projectile.ChildEventsRemaining)) return;
        projectile.ChildEventsRemaining--;
        if (projectile.Modules.HasFlag(ProjectileModules.ExplosionAction) ||
            projectile.Modules.HasFlag(ProjectileModules.Nova) ||
            projectile.Modules.HasFlag(ProjectileModules.GroundField))
        {
            SubmitModuleArea(projectile, center,
                projectile.Modules.HasFlag(ProjectileModules.GroundField) ? 3.6f : 2.8f,
                projectile.Modules.HasFlag(ProjectileModules.GroundField) ? 0.42f : 0.7f);
        }
        if (projectile.Modules.HasFlag(ProjectileModules.GroundField))
        {
            FieldRequested?.Invoke(center, projectile.SourceSpellId, projectile.Damage,
                projectile.Element, projectile.ElementStacks, projectile.ChainDepth + 1);
        }
        if (projectile.Modules.HasFlag(ProjectileModules.Fragments))
        {
            SpawnFragments(projectile, center, 4, 0.45f);
        }
        if (projectile.Modules.HasFlag(ProjectileModules.StatusSpread))
        {
            SubmitModuleArea(projectile, center, 3.5f, 0.0f);
        }
    }

    private void SpawnFragments(in ProjectileState parent, Vector2 center, int count, float damageScale)
    {
        if (!ProjectileEffectBudget.CanSpawnChild(parent.ChainDepth, parent.ChildEventsRemaining)) return;
        ProjectileModules childModules = parent.Modules & ~(ProjectileModules.Split |
            ProjectileModules.Fragments | ProjectileModules.TriggerOnPierce |
            ProjectileModules.TriggerOnBounce);
        for (int index = 0; index < count && index < 8; index++)
        {
            Vector2 direction = Vector2.FromAngle(Mathf.Tau * index / count);
            Spawn(center, direction, parent.Damage * damageScale,
                Math.Max(8.0f, parent.Velocity.Length() * 0.85f), 0.8f,
                parent.SourceSpellId, parent.Element, parent.ElementStacks, 1, 0.0f,
                0, 0.0f, false, childModules, parent.ChainDepth + 1,
                parent.ChildEventsRemaining - 1, parent.SecondaryElement, parent.Fusion);
        }
    }

    private void SubmitModuleArea(in ProjectileState projectile, Vector2 center, float radius, float damageScale)
    {
        _grid.QueryCircle(center, radius, _splashCandidates);
        int submitted = 0;
        foreach (EntityHandle candidate in _splashCandidates)
        {
            if (submitted >= 8) break;
            _combat.Submit(new DamageContext(candidate, projectile.SourceSpellId,
                projectile.Damage * damageScale, projectile.Element, projectile.ElementStacks,
                projectile.Flags, projectile.ChainDepth + 1,
                projectile.SecondaryElement, projectile.Fusion));
            submitted++;
        }
    }

    private bool TryRedirectToBounceTarget(ref ProjectileState projectile, Vector2 origin)
    {
        _grid.QueryCircle(origin, projectile.BounceRange, _candidates);
        EntityHandle best = EntityHandle.Invalid;
        Vector2 bestPosition = default;
        float bestDistance = float.MaxValue;
        bool bestIsMarked = false;
        foreach (EntityHandle candidate in _candidates)
        {
            if (HasHit(projectile, candidate) || !_enemies.TryGet(candidate, out EnemyState enemy))
            {
                continue;
            }
            bool isMarked = PreferMarkedTargets && enemy.Elements.Mark.Stacks > 0;
            float distance = origin.DistanceSquaredTo(enemy.Position);
            if (!best.IsValid || (isMarked, distance) switch
                {
                    (true, _) when !bestIsMarked => true,
                    (true, _) when bestIsMarked => distance < bestDistance,
                    (false, _) when bestIsMarked => false,
                    _ => distance < bestDistance
                })
            {
                best = candidate;
                bestPosition = enemy.Position;
                bestDistance = distance;
                bestIsMarked = isMarked;
            }
        }
        if (!best.IsValid)
        {
            return false;
        }
        float speed = projectile.Velocity.Length();
        projectile.Position = origin;
        projectile.PreviousPosition = origin;
        projectile.Velocity = (bestPosition - origin).Normalized() * speed;
        return true;
    }

    private void RemoveAt(int index)
    {
        int lastIndex = ActiveCount - 1;
        if (index != lastIndex)
        {
            _projectiles[index] = _projectiles[lastIndex];
        }
        _projectiles[lastIndex] = default;
        ActiveCount--;
    }

    private void BuildPresentation()
    {
        Mesh mesh = VfxMaterials.BuildBillboardMesh(0.62f, "magic_03", new Color(0.3f, 0.65f, 1.0f));

        _multiMesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = mesh,
            InstanceCount = Capacity,
            VisibleInstanceCount = 0
        };
        _view.Multimesh = _multiMesh;
    }

    private void SyncPresentation()
    {
        for (int index = 0; index < ActiveCount; index++)
        {
            ProjectileState projectile = _projectiles[index];
            Vector3 origin = new(projectile.Position.X, 0.8f, projectile.Position.Y);
            _multiMesh.SetInstanceTransform(index, new Transform3D(Basis.Identity, origin));
            _multiMesh.SetInstanceColor(index, ElementPalette.For(projectile.Element));
        }
        _multiMesh.VisibleInstanceCount = ActiveCount;
    }

    private void SubmitExplosion(
        in ProjectileState projectile,
        Vector2 center,
        EntityHandle primary)
    {
        _grid.QueryCircle(center, projectile.ExplosionRadius, _splashCandidates);
        bool primarySubmitted = false;
        foreach (EntityHandle candidate in _splashCandidates)
        {
            bool isPrimary = candidate == primary;
            SubmitHit(projectile, candidate, projectile.Damage * (isPrimary ? 1.0f : 0.72f));
            primarySubmitted |= isPrimary;
        }
        if (!primarySubmitted)
        {
            SubmitHit(projectile, primary, projectile.Damage);
        }
    }

    private void SubmitExplosionAt(in ProjectileState projectile, Vector2 center)
    {
        _grid.QueryCircle(center, projectile.ExplosionRadius, _splashCandidates);
        foreach (EntityHandle candidate in _splashCandidates)
        {
            SubmitHit(projectile, candidate, projectile.Damage);
        }
    }

    private void SubmitHit(in ProjectileState projectile, EntityHandle target, float damage)
    {
        _combat.Submit(new DamageContext(
            target,
            projectile.SourceSpellId,
            damage,
            projectile.Element,
            projectile.ElementStacks,
            projectile.Flags,
            projectile.ChainDepth,
            projectile.SecondaryElement,
            projectile.Fusion));
    }

    private static bool HasHit(in ProjectileState projectile, EntityHandle target) =>
        projectile.Hit0 == target || projectile.Hit1 == target ||
        projectile.Hit2 == target || projectile.Hit3 == target ||
        projectile.Hit4 == target || projectile.Hit5 == target ||
        projectile.Hit6 == target || projectile.Hit7 == target;

    private static void RecordHit(ref ProjectileState projectile, EntityHandle target)
    {
        if (!projectile.Hit0.IsValid)
        {
            projectile.Hit0 = target;
        }
        else if (!projectile.Hit1.IsValid)
        {
            projectile.Hit1 = target;
        }
        else if (!projectile.Hit2.IsValid)
        {
            projectile.Hit2 = target;
        }
        else if (!projectile.Hit3.IsValid)
        {
            projectile.Hit3 = target;
        }
        else if (!projectile.Hit4.IsValid)
        {
            projectile.Hit4 = target;
        }
        else if (!projectile.Hit5.IsValid)
        {
            projectile.Hit5 = target;
        }
        else if (!projectile.Hit6.IsValid)
        {
            projectile.Hit6 = target;
        }
        else
        {
            projectile.Hit7 = target;
        }
    }

    private static void ClearHits(ref ProjectileState projectile)
    {
        projectile.Hit0 = EntityHandle.Invalid;
        projectile.Hit1 = EntityHandle.Invalid;
        projectile.Hit2 = EntityHandle.Invalid;
        projectile.Hit3 = EntityHandle.Invalid;
        projectile.Hit4 = EntityHandle.Invalid;
        projectile.Hit5 = EntityHandle.Invalid;
        projectile.Hit6 = EntityHandle.Invalid;
        projectile.Hit7 = EntityHandle.Invalid;
    }

    private static float DistanceSquaredToSegment(Vector2 point, Vector2 start, Vector2 end)
    {
        Vector2 segment = end - start;
        float segmentLengthSquared = segment.LengthSquared();
        if (segmentLengthSquared <= 0.000001f)
        {
            return point.DistanceSquaredTo(start);
        }
        float t = Mathf.Clamp((point - start).Dot(segment) / segmentLengthSquared, 0.0f, 1.0f);
        Vector2 closest = start + segment * t;
        return point.DistanceSquaredTo(closest);
    }
}
