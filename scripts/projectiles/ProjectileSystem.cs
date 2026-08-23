using Catalyst.Combat;
using Catalyst.Core;
using Catalyst.Elements;
using Catalyst.Enemies;
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
    private MultiMeshInstance3D _view = null!;
    private MultiMesh _multiMesh = null!;

    public int ActiveCount { get; private set; }

    public override void _Ready()
    {
        _enemies = GetNode<EnemySystem>("../EntitySystem");
        _grid = GetNode<SpatialGrid>("../SpatialGrid");
        _run = GetNode<RunController>("../RunController");
        _combat = GetNode<CombatSystem>("../CombatSystem");
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
        FusionIdentity fusion = FusionIdentity.None) =>
        Spawn(position, Vector2.Right, damage, 0.0f, fuseSeconds, sourceSpellId,
            element, elementStacks, 1, explosionRadius, 0, 0.0f, true, modules,
            secondaryElement: secondaryElement, fusion: fusion);

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
        foreach (EntityHandle candidate in _candidates)
        {
            if (HasHit(projectile, candidate) || !_enemies.TryGet(candidate, out EnemyState enemy))
            {
                continue;
            }
            float distance = origin.DistanceSquaredTo(enemy.Position);
            if (distance < bestDistance)
            {
                best = candidate;
                bestPosition = enemy.Position;
                bestDistance = distance;
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
        SphereMesh mesh = new()
        {
            Radius = 0.2f,
            Height = 0.4f,
            RadialSegments = 8,
            Rings = 4
        };
        mesh.Material = new StandardMaterial3D
        {
            AlbedoColor = Colors.White,
            EmissionEnabled = true,
            Emission = new Color(0.3f, 0.65f, 1.0f),
            EmissionEnergyMultiplier = 2.2f,
            Roughness = 0.25f,
            VertexColorUseAsAlbedo = true
        };

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
            _multiMesh.SetInstanceColor(index, GetElementColor(projectile.Element));
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

    private static Color GetElementColor(ElementType element) => element switch
    {
        ElementType.Fire => new Color(1.0f, 0.28f, 0.05f),
        ElementType.Frost => new Color(0.2f, 0.82f, 1.0f),
        ElementType.Lightning => new Color(1.0f, 0.9f, 0.18f),
        ElementType.Water => new Color(0.12f, 0.48f, 1.0f),
        ElementType.Wind => new Color(0.45f, 1.0f, 0.72f),
        ElementType.Earth => new Color(0.72f, 0.44f, 0.16f),
        ElementType.Mark => new Color(0.9f, 0.28f, 1.0f),
        _ => new Color(0.42f, 0.82f, 1.0f)
    };

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
