using Catalyst.App;
using Catalyst.Combat;
using Catalyst.Elements;
using Catalyst.Enemies;
using Catalyst.Player;
using Catalyst.Projectiles;
using Catalyst.Run;
using Catalyst.Meta;
using Catalyst.Spatial;
using Catalyst.Spells;
using Godot;

namespace Catalyst.Passives;

/// <summary>
/// Runs the unified passive framework: drains the event bus each physics
/// frame, evaluates conditions with deterministic ordering and delegates
/// effects to the executor. Cooldowns only advance while the run is playing.
/// </summary>
public partial class PassiveSystem : Node
{
    private readonly PassiveEventBus _bus = new();
    private readonly List<PassiveEventContext> _snapshot = new(256);
    private readonly List<PassiveRuntime> _passives = new(32);
    private readonly List<PassiveRuntime> _matching = new(8);
    private readonly Dictionary<PassiveTriggerKind, List<PassiveRuntime>> _index = new();
    private readonly List<EntityHandle> _queryBuffer = new(64);

    private EnemySystem _enemies = null!;
    private SpatialGrid _grid = null!;
    private CombatSystem _combat = null!;
    private ElementSystem _elements = null!;
    private SpellSystem _spells = null!;
    private ProjectileSystem _projectiles = null!;
    private AutoCatalysisSystem _catalysis = null!;
    private RunController _run = null!;
    private RunStatistics _statistics = null!;
    private PlayerController _player = null!;
    private PlayerHealth _health = null!;
    private PassiveEffectExecutor _executor = null!;

    public int ActivePassiveCount => _passives.Count;
    public int EventsDispatchedThisFrame => _bus.EventsThisFrame;
    public int DroppedEventCount => _bus.DroppedEventCount;
    public PassiveEventBus Bus => _bus;

    public override void _Ready()
    {
        _enemies = GetNode<EnemySystem>("../EntitySystem");
        _grid = GetNode<SpatialGrid>("../SpatialGrid");
        _combat = GetNode<CombatSystem>("../CombatSystem");
        _elements = GetNode<ElementSystem>("../ElementSystem");
        _spells = GetNode<SpellSystem>("../SpellSystem");
        _projectiles = GetNode<ProjectileSystem>("../ProjectileSystem");
        _catalysis = GetNode<AutoCatalysisSystem>("../AutoCatalysisSystem");
        _run = GetNode<RunController>("../RunController");
        _statistics = GetNode<RunStatistics>("../RunStatistics");
        _player = GetNode<PlayerController>("../../WorldRoot/Player");
        _health = GetNode<PlayerHealth>("../../WorldRoot/Player/HealthComponent");
        _executor = new PassiveEffectExecutor(
            _enemies, _grid, _combat, _spells, _projectiles, _catalysis);

        CharacterDefinition character = GetNode<SaveService>("/root/SaveService").ActiveCharacter;
        BuildPassives(character);

        _spells.SpellCast += OnSpellCast;
        _spells.LoadoutChanged += OnLoadoutChanged;
        _combat.HitResolved += OnHitResolved;
        _elements.ReactionTriggered += OnReactionTriggered;
        _elements.Frozen += OnFrozen;
        _elements.ElementApplied += OnElementApplied;
        _catalysis.Executed += OnCatalysisExecuted;
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_spells))
        {
            _spells.SpellCast -= OnSpellCast;
            _spells.LoadoutChanged -= OnLoadoutChanged;
        }
        if (IsInstanceValid(_combat)) _combat.HitResolved -= OnHitResolved;
        if (IsInstanceValid(_elements))
        {
            _elements.ReactionTriggered -= OnReactionTriggered;
            _elements.Frozen -= OnFrozen;
            _elements.ElementApplied -= OnElementApplied;
        }
        if (IsInstanceValid(_catalysis)) _catalysis.Executed -= OnCatalysisExecuted;
    }

    public override void _PhysicsProcess(double deltaValue)
    {
        _bus.BeginFrame();
        if (_run.State != RunState.Playing)
        {
            return;
        }

        float delta = (float)deltaValue;
        double now = _run.ElapsedSeconds;
        UpdateTimers(delta, now);
        _bus.Drain(_snapshot);
        ProcessEvents(now);
        _bus.EndFrame();
    }

    private void BuildPassives(CharacterDefinition character)
    {
        _passives.Clear();
        _index.Clear();
        foreach (PassiveBlueprint definition in PassiveCatalog.GetMany(character.CorePassiveIds))
        {
            PassiveRuntime runtime = new(definition);
            _passives.Add(runtime);
            if (!_index.TryGetValue(definition.Trigger, out List<PassiveRuntime>? bucket))
            {
                bucket = new List<PassiveRuntime>();
                _index[definition.Trigger] = bucket;
            }
            bucket.Add(runtime);
        }

        // Configuration passives reshape the automatic systems once at start.
        foreach (PassiveRuntime runtime in _passives)
        {
            switch (runtime.Definition.Id.ToString())
            {
                case PassiveCatalog.ElementalistCatalysisOptimizer:
                    _catalysis.ApplyOptimization(0.85f, 0.90f);
                    break;
                case PassiveCatalog.EchoHunterMarkPriority:
                    _projectiles.PreferMarkedTargets = true;
                    break;
                case PassiveCatalog.EchoHunterAutoRecall:
                    _projectiles.AutoRecallEnabled = true;
                    break;
                case PassiveCatalog.RiftEngineerCapacitySafety:
                    _projectiles.CapacitySafetyEnabled = true;
                    break;
            }
        }
    }

    private void UpdateTimers(float delta, double now)
    {
        foreach (PassiveRuntime passive in _passives)
        {
            passive.CooldownRemaining = Math.Max(0.0f, passive.CooldownRemaining - delta);
            if (passive.Definition.Trigger == PassiveTriggerKind.Interval)
            {
                passive.IntervalRemaining -= delta;
                if (passive.IntervalRemaining <= 0.0f)
                {
                    passive.IntervalRemaining = passive.Definition.IntervalSeconds;
                    Publish(passive, PassiveEventContext.Create(
                        PassiveTriggerKind.Interval, now, _bus.NextSequence(),
                        position: GetPlayerPosition()));
                }
            }
        }
    }

    private void ProcessEvents(double now)
    {
        foreach (PassiveEventContext context in _snapshot)
        {
            if (!_index.TryGetValue(context.Trigger, out List<PassiveRuntime>? bucket))
            {
                continue;
            }

            _matching.Clear();
            _matching.AddRange(bucket);
            // Deterministic order: priority descending, stable id ascending.
            _matching.Sort((left, right) =>
            {
                int order = right.Definition.Priority.CompareTo(left.Definition.Priority);
                return order != 0
                    ? order
                    : string.Compare(left.Definition.Id.ToString(), right.Definition.Id.ToString(),
                        StringComparison.Ordinal);
            });

            foreach (PassiveRuntime passive in _matching)
            {
                ProcessPassive(passive, context, now);
            }
        }
    }

    private void ProcessPassive(PassiveRuntime passive, in PassiveEventContext context, double now)
    {
        if (!passive.Enabled ||
            passive.Definition.Trigger == PassiveTriggerKind.None ||
            passive.LastProcessedSequence == context.Sequence ||
            passive.LastTriggeredSequence == context.Sequence && !passive.Definition.AllowSelfTriggeredEvents)
        {
            return;
        }
        passive.LastProcessedSequence = context.Sequence;

        if (context.Trigger == PassiveTriggerKind.Interval)
        {
            // Interval events already target their own passive instance.
        }
        else if (passive.Definition.Trigger != context.Trigger)
        {
            return;
        }

        if (passive.CooldownRemaining > 0.0f || !IsRateWindowOpen(passive, now))
        {
            return;
        }
        if (!EvaluateCondition(passive, context))
        {
            // A failed condition never consumes the cooldown.
            return;
        }

        PassiveEventContext executionContext = context;
        if (context.Trigger == PassiveTriggerKind.Interval)
        {
            executionContext = RetargetIntervalEvent(passive, context);
        }
        if (_executor.Execute(passive, executionContext, out float producedValue))
        {
            passive.CooldownRemaining = passive.Definition.CooldownSeconds;
            passive.TotalTriggerCount++;
            passive.TotalValueProduced += producedValue;
            passive.LastTriggeredSequence = context.Sequence;
            _statistics.RecordPassiveTrigger(passive.Definition.Id.ToString(), producedValue);
        }
    }

    private PassiveEventContext RetargetIntervalEvent(PassiveRuntime passive, in PassiveEventContext context)
    {
        // Interval passives scan for their own target (e.g. the highest value
        // elite in range) because no gameplay event carried one.
        EntityHandle target = EntityHandle.Invalid;
        Vector2 position = context.Position;
        switch (passive.Definition.Effect)
        {
            case PassiveEffectKind.OverloadNearestDeployment:
            {
                float bestDistance = float.MaxValue;
                for (int index = 0; index < _enemies.ActiveCount; index++)
                {
                    if (!_enemies.TryGet(_enemies.GetHandleAtDenseIndex(index), out EnemyState enemy))
                    {
                        continue;
                    }
                    if (!enemy.IsElite && enemy.Archetype != EnemyArchetype.BossAshenColossus)
                    {
                        continue;
                    }
                    float distance = enemy.Position.DistanceTo(position);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        target = enemy.Handle;
                        position = enemy.Position;
                    }
                }
                break;
            }
            case PassiveEffectKind.CreateDeployment:
            {
                // Steady mine: place toward the nearest enemy, 3 m from the player.
                EntityHandle nearest = _grid.TryFindNearest(position, 12.0f, out EntityHandle found) &&
                    _enemies.TryGet(found, out _)
                    ? found
                    : EntityHandle.Invalid;
                if (_enemies.TryGet(nearest, out EnemyState nearestEnemy))
                {
                    Vector2 offset = nearestEnemy.Position - position;
                    float length = offset.Length();
                    position += length > 0.01f
                        ? offset / length * Math.Min(3.0f, length)
                        : Vector2.Zero;
                }
                break;
            }
        }
        return context with { Target = target, Position = position };
    }

    private bool IsRateWindowOpen(PassiveRuntime passive, double now)
    {
        if (now - passive.RateWindowStartedAt >= 1.0)
        {
            passive.RateWindowStartedAt = now;
            passive.TriggersThisSecond = 0;
        }
        if (passive.TriggersThisSecond >= passive.Definition.MaximumTriggersPerSecond)
        {
            return false;
        }
        passive.TriggersThisSecond++;
        return true;
    }

    private bool EvaluateCondition(PassiveRuntime passive, in PassiveEventContext context)
    {
        return passive.Definition.Condition switch
        {
            PassiveConditionKind.Always or PassiveConditionKind.CooldownReady => true,
            PassiveConditionKind.TargetAlive => _enemies.TryGet(context.Target, out _),
            PassiveConditionKind.TargetIsMarked => _enemies.TryGet(context.Target, out EnemyState marked) &&
                marked.Elements.Mark.Stacks > 0,
            PassiveConditionKind.TargetIsElite => _enemies.TryGet(context.Target, out EnemyState elite) &&
                (elite.IsElite || elite.Archetype == EnemyArchetype.BossAshenColossus),
            PassiveConditionKind.TargetHasElement => _enemies.TryGet(context.Target, out EnemyState withElement) &&
                ElementResolver.GetStacks(withElement.Elements, passive.Definition.RequiredElement) > 0,
            PassiveConditionKind.TargetHasReactionPotential => _enemies.TryGet(context.Target, out EnemyState reactive) &&
                ElementSystem.TryPreviewCatalysis(reactive.Elements).CanReact,
            PassiveConditionKind.MinimumNearbyEnemies => CountNearby(context.Position, passive.Definition.Radius) >=
                (int)passive.Definition.Value,
            PassiveConditionKind.MinimumNearbyReactionTargets => CountNearbyReactive(context.Position,
                passive.Definition.Radius) >= (int)passive.Definition.Value,
            PassiveConditionKind.DeploymentBelowLimit => _projectiles.ActiveMineCount < _projectiles.DeploymentLimit,
            PassiveConditionKind.DeploymentAtLimit => _projectiles.ActiveMineCount >= _projectiles.DeploymentLimit,
            PassiveConditionKind.ChargeFull => _catalysis.Charge >= 1.0f,
            PassiveConditionKind.PlayerHealthBelowFraction =>
                _health.MaxHealth <= 0.0f || _health.CurrentHealth / _health.MaxHealth <= passive.Definition.Value,
            PassiveConditionKind.WeaponMatches => string.IsNullOrEmpty(passive.Definition.TargetWeaponId) ||
                passive.Definition.TargetWeaponId == context.WeaponId,
            PassiveConditionKind.RandomProc => _run.RandomStreams.PassiveProc.Randf() <= passive.Definition.Value,
            PassiveConditionKind.PayloadDiscountAvailable => !_spells.PayloadDiscountUsed,
            _ => false
        };
    }

    private int CountNearby(Vector2 position, float radius)
    {
        _grid.QueryCircle(position, radius, _queryBuffer);
        int count = 0;
        foreach (EntityHandle handle in _queryBuffer)
        {
            count += _enemies.TryGet(handle, out _) ? 1 : 0;
        }
        return count;
    }

    private int CountNearbyReactive(Vector2 position, float radius)
    {
        _grid.QueryCircle(position, radius, _queryBuffer);
        int count = 0;
        foreach (EntityHandle handle in _queryBuffer)
        {
            if (_enemies.TryGet(handle, out EnemyState enemy) &&
                ElementSystem.TryPreviewCatalysis(enemy.Elements).CanReact)
            {
                count++;
            }
        }
        return count;
    }

    private void Publish(PassiveRuntime source, in PassiveEventContext context)
    {
        PassiveEventContext derived = context with
        {
            SourceDefinitionId = source.Definition.Id,
            ChainDepth = context.ChainDepth + 1,
            RootSequence = context.RootSequence == 0 ? context.Sequence : context.RootSequence
        };
        _bus.Publish(derived);
    }

    private void OnSpellCast(SpellCastKind kind)
    {
        _bus.Publish(PassiveEventContext.Create(
            PassiveTriggerKind.WeaponCast, _run.ElapsedSeconds, _bus.NextSequence(),
            position: GetPlayerPosition(),
            weaponId: new StringName(kind.ToString())));
    }

    private void OnLoadoutChanged()
    {
        _bus.Publish(PassiveEventContext.Create(
            PassiveTriggerKind.LoadoutChanged, _run.ElapsedSeconds, _bus.NextSequence()));
    }

    private void OnHitResolved(DamageContext context, EnemyState beforeHit, bool killed)
    {
        PassiveTriggerKind trigger = killed ? PassiveTriggerKind.EnemyKilled : PassiveTriggerKind.ProjectileHit;
        _bus.Publish(PassiveEventContext.Create(
            trigger, _run.ElapsedSeconds, _bus.NextSequence(),
            position: beforeHit.Position,
            target: context.Target,
            weaponId: context.SourceSpellId,
            value: context.BaseDamage,
            count: context.ElementStacks,
            element: context.Element,
            flags: context.Flags,
            chainDepth: context.ChainDepth));
    }

    private void OnReactionTriggered(ReactionKind kind, Vector2 position, float damage)
    {
        _bus.Publish(PassiveEventContext.Create(
            PassiveTriggerKind.ReactionTriggered, _run.ElapsedSeconds, _bus.NextSequence(),
            position: position,
            value: damage,
            reaction: kind));
    }

    private void OnFrozen(EntityHandle target, DamageContext context)
    {
        _bus.Publish(PassiveEventContext.Create(
            PassiveTriggerKind.EnemyFrozen, _run.ElapsedSeconds, _bus.NextSequence(),
            target: target,
            weaponId: context.SourceSpellId));
    }

    private void OnElementApplied(EntityHandle target, ElementType element)
    {
        _bus.Publish(PassiveEventContext.Create(
            PassiveTriggerKind.ElementApplied, _run.ElapsedSeconds, _bus.NextSequence(),
            target: target,
            element: element));
    }

    private void OnCatalysisExecuted(Vector2 position, int reactionCount, float damage)
    {
        _bus.Publish(PassiveEventContext.Create(
            PassiveTriggerKind.CatalysisExecuted, _run.ElapsedSeconds, _bus.NextSequence(),
            position: position,
            value: damage,
            count: reactionCount));
    }

    private Vector2 GetPlayerPosition() => new(_player.GlobalPosition.X, _player.GlobalPosition.Z);
}
