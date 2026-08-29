using Catalyst.Combat;
using Catalyst.Core;
using Catalyst.Enemies;
using Catalyst.Run;
using Catalyst.Spatial;
using Catalyst.App;
using Godot;

namespace Catalyst.Elements;

/// <summary>Read-only catalysis preview. Never mutates the evaluated state.</summary>
public readonly record struct ReactionPreview(
    bool CanReact,
    ReactionKind Reaction,
    int ConsumedStacks,
    float ExpectedDamage);

public partial class ElementSystem : Node
{
    public event Action<ReactionKind, Vector2, float>? ReactionTriggered;
    public event Action<EntityHandle, ReactionKind, float>? ReactionResolved;
    public event Action<EntityHandle, DamageContext>? Frozen;
    public event Action<FusionIdentity, Vector2>? ChemistryReactionTriggered;
    public event Action<EntityHandle, ElementType>? ElementApplied;

    private readonly List<EntityHandle> _candidates = new(64);
    private EnemySystem _enemies = null!;
    private SpatialGrid _grid = null!;
    private CombatSystem _combat = null!;
    private RunController _run = null!;
    private RunStatistics _statistics = null!;
    private ElementResolver _resolver = null!;

    public override void _Ready()
    {
        _enemies = GetNode<EnemySystem>("../EntitySystem");
        _grid = GetNode<SpatialGrid>("../SpatialGrid");
        _combat = GetNode<CombatSystem>("../CombatSystem");
        _run = GetNode<RunController>("../RunController");
        _statistics = GetNode<RunStatistics>("../RunStatistics");

        List<ReactionRule> rules = new();
        ContentCatalog catalog = GetNode<ContentCatalog>("/root/ContentCatalog");
        _resolver = new ElementResolver(rules);
    }

    public override void _PhysicsProcess(double deltaValue)
    {
        if (_run.State != RunState.Playing)
        {
            return;
        }

        float delta = (float)deltaValue;
        int denseIndex = 0;
        while (denseIndex < _enemies.ActiveCount)
        {
            EntityHandle handle = _enemies.GetHandleAtDenseIndex(denseIndex);
            if (!_enemies.TryGet(handle, out EnemyState enemy))
            {
                denseIndex++;
                continue;
            }

            ElementRuntimeState runtime = enemy.Elements;
            float burnDamage = _resolver.Tick(ref runtime, delta);
            _enemies.SetElementRuntime(handle, runtime);
            if (burnDamage > 0.0f)
            {
                _enemies.ApplyDamage(handle, burnDamage);
            }

            if (_enemies.TryGet(handle, out _))
            {
                denseIndex++;
            }
        }
    }

    public void ApplyElement(in DamageContext context, bool canTriggerReaction)
    {
        if (!_enemies.TryGet(context.Target, out EnemyState enemy))
        {
            return;
        }

        ElementRuntimeState runtime = enemy.Elements;
        bool wasWet = runtime.Water.Stacks > 0;
        bool wasBurning = runtime.Fire.Stacks > 0;
        bool wasFrozen = runtime.FrozenRemaining > 0.0f;
        bool isMine = context.SourceSpellId.ToString() == "weapon.element_mine";

        if (!isMine && context.ChainDepth == 0)
        {
            if ((context.Element == ElementType.Fire || context.SecondaryElement == ElementType.Fire) && wasWet ||
                (context.Element == ElementType.Water || context.SecondaryElement == ElementType.Water) && wasBurning)
            {
                ExecuteSteamShock(context.Target, context, ref runtime);
            }
            else if ((context.Element == ElementType.Lightning || context.SecondaryElement == ElementType.Lightning) && wasWet)
            {
                ExecuteConduction(context.Target, context);
                runtime.Water = default;
            }
        }

        int appliedStacks = enemy.ResistantElement == context.Element
            ? Math.Max(1, context.ElementStacks - 1)
            : context.ElementStacks;
        if (ShouldApply(context.Element, context.Fusion))
        {
            _resolver.ApplyElement(ref runtime, context.Element, appliedStacks, false, out _);
        }
        if (context.SecondaryElement != ElementType.None && ShouldApply(context.SecondaryElement, context.Fusion))
        {
            _resolver.ApplyElement(ref runtime, context.SecondaryElement, appliedStacks, false, out _);
        }
        ApplyFusionSignature(ref runtime, context.Fusion);
        if (enemy.Archetype == EnemyArchetype.BossAshenColossus)
        {
            ScaleAppliedDurationForBoss(ref runtime, context.Element);
            ScaleAppliedDurationForBoss(ref runtime, context.SecondaryElement);
            if (context.Fusion == FusionIdentity.Ice) runtime.FrozenRemaining *= 0.25f;
            if (context.Fusion == FusionIdentity.Steam) runtime.SteamScaldRemaining *= 0.25f;
        }
        if (!wasFrozen && runtime.FrozenRemaining > 0.0f)
        {
            Frozen?.Invoke(context.Target, context);
        }
        if (ShouldApply(context.Element, context.Fusion))
        {
            ElementApplied?.Invoke(context.Target, context.Element);
        }
        if (context.SecondaryElement != ElementType.None &&
            ShouldApply(context.SecondaryElement, context.Fusion))
        {
            ElementApplied?.Invoke(context.Target, context.SecondaryElement);
        }
        _enemies.SetElementRuntime(context.Target, runtime);
    }

    /// <summary>
    /// Read-only preview of the catalysis result for one element state. The
    /// estimate mirrors <see cref="ForceReaction"/> resolution order and never
    /// mutates <paramref name="state"/>.
    /// </summary>
    public static ReactionPreview TryPreviewCatalysis(in ElementRuntimeState state)
    {
        int water = state.Water.Stacks;
        int fire = state.Fire.Stacks;
        int lightning = state.Lightning.Stacks;
        if (water > 0 && fire > 0)
        {
            int consumed = Math.Min(water, fire);
            return new ReactionPreview(true, ReactionKind.SteamShock, consumed,
                2.0f + 0.65f * consumed);
        }
        if (water > 0 && lightning > 0)
        {
            // Conduction damages every wet target in range; the estimate covers
            // the initiating target only and stays deterministic.
            return new ReactionPreview(true, ReactionKind.Conduction,
                Math.Min(water, lightning), 2.35f);
        }
        return default;
    }

    private bool ShouldApply(ElementType element, FusionIdentity fusion)
    {
        if (element == ElementType.None) return false;
        if (fusion == FusionIdentity.None && element is ElementType.Fire or ElementType.Frost or ElementType.Lightning)
            return true;
        float probability = element switch
        {
            ElementType.Fire => 0.70f,
            ElementType.Water => 0.70f,
            ElementType.Wind => 0.55f,
            ElementType.Earth => 0.60f,
            ElementType.Lightning => 0.60f,
            ElementType.Mark => 0.85f,
            _ => 1.0f
        };
        return _run.RandomStreams.GameplayProc.Randf() <= probability;
    }

    private static void ApplyFusionSignature(ref ElementRuntimeState runtime, FusionIdentity fusion)
    {
        switch (fusion)
        {
            case FusionIdentity.Ice:
                runtime.FrozenRemaining = Math.Max(runtime.FrozenRemaining, 0.75f);
                break;
            case FusionIdentity.Steam:
                runtime.SteamScaldRemaining = Math.Max(runtime.SteamScaldRemaining, 2.1f);
                break;
            case FusionIdentity.ConductiveLightning:
            case FusionIdentity.Plasma:
            case FusionIdentity.Storm:
                runtime.StaggerRemaining = Math.Max(runtime.StaggerRemaining, 0.12f);
                break;
        }
    }

    private static void ScaleAppliedDurationForBoss(ref ElementRuntimeState runtime, ElementType element)
    {
        switch (element)
        {
            case ElementType.Fire: runtime.Fire.RemainingDuration *= 0.25f; break;
            case ElementType.Frost: runtime.Frost.RemainingDuration *= 0.25f; break;
            case ElementType.Lightning: runtime.Lightning.RemainingDuration *= 0.25f; break;
            case ElementType.Water: runtime.Water.RemainingDuration *= 0.25f; break;
            case ElementType.Wind: runtime.Wind.RemainingDuration *= 0.25f; break;
            case ElementType.Earth: runtime.Earth.RemainingDuration *= 0.25f; break;
            case ElementType.Mark: runtime.Mark.RemainingDuration *= 0.25f; break;
        }
    }

    private void ExecuteSteamShock(EntityHandle target, in DamageContext context, ref ElementRuntimeState runtime)
    {
        runtime.Water = default;
        runtime.Fire = default;
        runtime.FrozenRemaining = 0.0f;
        runtime.SteamScaldRemaining = 0.0f;
        if (!_enemies.TryGet(target, out EnemyState primary)) return;
        float damage = Math.Max(2.0f, context.BaseDamage * 0.65f);
        _statistics.RecordReaction(ReactionKind.SteamShock, damage);
        ReactionTriggered?.Invoke(ReactionKind.SteamShock, primary.Position, damage);
        ReactionResolved?.Invoke(target, ReactionKind.SteamShock, damage);
        ChemistryReactionTriggered?.Invoke(FusionIdentity.Steam, primary.Position);
        _grid.QueryCircle(primary.Position, 3.0f, _candidates);
        foreach (EntityHandle candidate in _candidates.Take(8))
        {
            _combat.Submit(new DamageContext(candidate, context.SourceSpellId,
                damage, ElementType.None, 0,
                DamageFlags.IsReactionDamage, context.ChainDepth + 1));
        }
    }

    private void ExecuteConduction(EntityHandle target, in DamageContext context)
    {
        if (!_enemies.TryGet(target, out EnemyState primary)) return;
        _grid.QueryCircle(primary.Position, 8.0f, _candidates);
        List<(EntityHandle Handle, EnemyState Enemy)> wet = new();
        foreach (EntityHandle handle in _candidates)
        {
            if (_enemies.TryGet(handle, out EnemyState enemy) && enemy.Elements.Water.Stacks > 0)
            {
                wet.Add((handle, enemy));
            }
        }
        wet.Sort((left, right) =>
        {
            int distance = primary.Position.DistanceSquaredTo(left.Enemy.Position)
                .CompareTo(primary.Position.DistanceSquaredTo(right.Enemy.Position));
            return distance != 0 ? distance : left.Handle.Index.CompareTo(right.Handle.Index);
        });
        if (wet.Count > 16) wet.RemoveRange(16, wet.Count - 16);
        if (wet.Count == 0) return;
        float multiplier = 1.0f + 0.35f * (wet.Count - 1);
        float networkDamage = Math.Max(1.0f, context.BaseDamage) * multiplier;
        _statistics.RecordReaction(ReactionKind.Conduction, networkDamage * wet.Count);
        ReactionTriggered?.Invoke(ReactionKind.Conduction, primary.Position, networkDamage * wet.Count);
        ReactionResolved?.Invoke(target, ReactionKind.Conduction, networkDamage * wet.Count);
        ChemistryReactionTriggered?.Invoke(FusionIdentity.ConductiveLightning, primary.Position);
        foreach ((EntityHandle handle, EnemyState enemy) in wet)
        {
            ElementRuntimeState state = enemy.Elements;
            state.Water = default;
            state.Lightning.Stacks = Math.Max(1, state.Lightning.Stacks);
            state.Lightning.RemainingDuration = ElementResolver.GetStatusDuration(ElementType.Lightning);
            _enemies.SetElementRuntime(handle, state);
            _combat.Submit(new DamageContext(handle, context.SourceSpellId,
                networkDamage,
                ElementType.Lightning, 1, DamageFlags.IsReactionDamage, context.ChainDepth + 1));
        }
    }

    public void ForceReaction(in DamageContext context)
    {
        if (!_enemies.TryGet(context.Target, out EnemyState enemy))
        {
            return;
        }

        ElementRuntimeState runtime = enemy.Elements;
        if (runtime.Water.Stacks > 0 && runtime.Fire.Stacks > 0)
        {
            ExecuteSteamShock(context.Target, context, ref runtime);
            _enemies.SetElementRuntime(context.Target, runtime);
        }
        else if (runtime.Water.Stacks > 0 && runtime.Lightning.Stacks > 0)
        {
            ExecuteConduction(context.Target, context);
        }
    }

}
