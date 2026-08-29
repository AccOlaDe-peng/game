using Catalyst.Combat;
using Catalyst.Elements;
using Catalyst.Enemies;
using Catalyst.Projectiles;
using Catalyst.Run;
using Catalyst.Meta;
using Catalyst.Spatial;
using Catalyst.Spells;
using Godot;

namespace Catalyst.Passives;

/// <summary>
/// Executes the gameplay side of a successful passive trigger. Returns the
/// produced value for statistics (0 when the effect only changes behavior).
/// </summary>
public sealed class PassiveEffectExecutor
{
    private readonly EnemySystem _enemies;
    private readonly SpatialGrid _grid;
    private readonly CombatSystem _combat;
    private readonly SpellSystem _spells;
    private readonly ProjectileSystem _projectiles;
    private readonly AutoCatalysisSystem _catalysis;
    private readonly List<EntityHandle> _queryBuffer = new(64);

    public PassiveEffectExecutor(
        EnemySystem enemies,
        SpatialGrid grid,
        CombatSystem combat,
        SpellSystem spells,
        ProjectileSystem projectiles,
        AutoCatalysisSystem catalysis)
    {
        _enemies = enemies;
        _grid = grid;
        _combat = combat;
        _spells = spells;
        _projectiles = projectiles;
        _catalysis = catalysis;
    }

    public bool Execute(PassiveRuntime passive, in PassiveEventContext context, out float producedValue)
    {
        producedValue = 0.0f;
        return passive.Definition.Effect switch
        {
            PassiveEffectKind.ApplyPayloadDiscount => _spells.ApplyPayloadDiscount(),
            PassiveEffectKind.SpreadMark => SpreadMark(passive, context, out producedValue),
            PassiveEffectKind.CreateDeployment => CreateWeakenedMine(passive, context),
            PassiveEffectKind.OverloadNearestDeployment => OverloadNearestDeployment(passive, context),
            PassiveEffectKind.TriggerOldestDeployment => _projectiles.TriggerOldestMine(),
            PassiveEffectKind.TriggerNearestDeployment => _projectiles.TriggerNearestMine(context.Position),
            PassiveEffectKind.ExecuteCatalysis => ExecuteCatalysis(context),
            PassiveEffectKind.ModifyCatalysisCharge => ModifyCatalysisCharge(passive),
            PassiveEffectKind.SpawnProjectile => SpawnRadialBurst(passive, context),
            PassiveEffectKind.EmitReactionAction => EmitReactionAction(passive, context, out producedValue),
            PassiveEffectKind.ApplyElement or PassiveEffectKind.ApplyMark => ApplyElementToTarget(passive, context),
            PassiveEffectKind.ReduceCooldown => ReduceWeaponCooldown(passive, context),
            _ => false
        };
    }

    private bool SpreadMark(PassiveRuntime passive, in PassiveEventContext context, out float producedValue)
    {
        producedValue = 0.0f;
        if (!_enemies.TryGet(context.Target, out EnemyState killed) || killed.Elements.Mark.Stacks <= 0)
        {
            return false;
        }

        _grid.QueryCircle(killed.Position, passive.Definition.Radius, _queryBuffer);
        List<(EntityHandle Handle, EnemyState Enemy)> candidates = new();
        foreach (EntityHandle candidate in _queryBuffer)
        {
            if (candidate == context.Target ||
                !_enemies.TryGet(candidate, out EnemyState enemy) ||
                enemy.Elements.Mark.Stacks > 0)
            {
                continue;
            }
            candidates.Add((candidate, enemy));
        }
        // Propagation priority: distance, health fraction, entity id.
        candidates.Sort((left, right) =>
        {
            int order = killed.Position.DistanceSquaredTo(left.Enemy.Position)
                .CompareTo(killed.Position.DistanceSquaredTo(right.Enemy.Position));
            if (order != 0) return order;
            float leftFraction = left.Enemy.Health / Math.Max(1f, left.Enemy.MaxHealth);
            float rightFraction = right.Enemy.Health / Math.Max(1f, right.Enemy.MaxHealth);
            order = leftFraction.CompareTo(rightFraction);
            return order != 0 ? order : left.Handle.Index.CompareTo(right.Handle.Index);
        });

        int spread = 0;
        int maximum = Math.Max(1, passive.Definition.Count);
        foreach ((EntityHandle handle, EnemyState enemy) in candidates)
        {
            if (spread >= maximum)
            {
                break;
            }
            ElementRuntimeState state = enemy.Elements;
            state.Mark.Stacks += (int)Math.Max(1, passive.Definition.Value);
            state.Mark.RemainingDuration = ElementResolver.GetStatusDuration(ElementType.Mark);
            _enemies.SetElementRuntime(handle, state);
            spread++;
        }
        producedValue = spread;
        return spread > 0;
    }

    private bool CreateWeakenedMine(PassiveRuntime passive, in PassiveEventContext context)
    {
        if (!_spells.TryGetRuntime(SpellCastKind.ElementMine, out SpellRuntime? runtime) || runtime is null)
        {
            return false;
        }
        if (!_projectiles.EnsureDeploymentCapacity())
        {
            return false;
        }
        SpellStats stats = runtime.Stats;
        bool spawned = _projectiles.SpawnMine(
            context.Position,
            stats.Damage * passive.Definition.Value,
            Math.Max(0.1f, stats.Lifetime),
            Math.Max(1.0f, stats.ExplosionRadius * passive.Definition.SecondaryValue),
            "passive.rift_engineer.steady_mine",
            stats.Element,
            stats.ElementStacks);
        return spawned;
    }

    private bool OverloadNearestDeployment(PassiveRuntime passive, in PassiveEventContext context)
    {
        if (!_enemies.TryGet(context.Target, out EnemyState elite) ||
            (!elite.IsElite && elite.Archetype != EnemyArchetype.BossAshenColossus))
        {
            return false;
        }

        // At least Value deployments must cover the elite before overloading.
        int required = Math.Max(1, (int)passive.Definition.Value);
        if (_projectiles.CountMinesCovering(elite.Position, 4.0f) < required)
        {
            return false;
        }
        if (!_projectiles.TriggerNearestMine(elite.Position))
        {
            return false;
        }
        _projectiles.NextMineDamageMultiplier = 1.25f;
        _projectiles.NextMineRadiusMultiplier = 1.15f;
        return true;
    }

    private bool ExecuteCatalysis(in PassiveEventContext context)
    {
        _catalysis.DebugTriggerAt(context.Position);
        return true;
    }

    private bool ModifyCatalysisCharge(PassiveRuntime passive)
    {
        _catalysis.DebugForceChargeFull();
        return true;
    }

    private bool SpawnRadialBurst(PassiveRuntime passive, in PassiveEventContext context)
    {
        _spells.SpawnPassiveRadial(
            context.Position,
            Math.Max(2, passive.Definition.Count),
            passive.Definition.SecondaryValue,
            passive.Definition.RequiredElement);
        return true;
    }

    private bool EmitReactionAction(PassiveRuntime passive, in PassiveEventContext context, out float producedValue)
    {
        producedValue = 0.0f;
        _grid.QueryCircle(context.Position, passive.Definition.Radius, _queryBuffer);
        int submitted = 0;
        foreach (EntityHandle target in _queryBuffer)
        {
            if (submitted >= 8)
            {
                break;
            }
            _combat.Submit(new DamageContext(
                target,
                passive.Definition.Id,
                passive.Definition.Value,
                ElementType.None,
                0,
                DamageFlags.IsReactionDamage,
                context.ChainDepth + 1));
            submitted++;
        }
        producedValue = submitted * passive.Definition.Value;
        return submitted > 0;
    }

    private bool ApplyElementToTarget(PassiveRuntime passive, in PassiveEventContext context)
    {
        if (!_enemies.TryGet(context.Target, out _))
        {
            return false;
        }
        _combat.Submit(new DamageContext(
            context.Target,
            passive.Definition.Id,
            passive.Definition.SecondaryValue,
            passive.Definition.RequiredElement,
            Math.Max(1, passive.Definition.Count),
            DamageFlags.CanTriggerReaction,
            context.ChainDepth + 1));
        return true;
    }

    private bool ReduceWeaponCooldown(PassiveRuntime passive, in PassiveEventContext context)
    {
        return _spells.ReduceCooldown(context.WeaponId, passive.Definition.Value);
    }
}
