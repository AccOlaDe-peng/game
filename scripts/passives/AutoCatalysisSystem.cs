using Catalyst.App;
using Catalyst.Combat;
using Catalyst.Elements;
using Catalyst.Enemies;
using Catalyst.Player;
using Catalyst.Run;
using Catalyst.Spatial;
using Godot;

namespace Catalyst.Passives;

public enum CatalysisState : byte
{
    Charging,
    Evaluating,
    ReadyWaiting,
    Executing,
    CooldownSuppressed
}

/// <summary>
/// The catalysis protocol: an expedition device that charges autonomously,
/// scores candidate reaction areas around the player and detonates the best
/// one once the score clears the safety threshold. Never wastes charge on
/// empty ground and never uses randomness.
/// </summary>
public partial class AutoCatalysisSystem : Node
{
    public event Action<Vector2, int, float>? Executed;
    public event Action? ChargeCompleted;

    [Export] public float ChargeDuration { get; set; } = 5.0f;
    [Export] public float SearchRadiusFromPlayer { get; set; } = 10.0f;
    [Export] public float EffectRadius { get; set; } = 4.5f;
    [Export] public float EvaluationInterval { get; set; } = 0.15f;
    [Export] public float MinimumScore { get; set; } = 3.0f;
    [Export] public int MinimumReactiveTargets { get; set; } = 2;
    [Export] public int MaximumCandidateCenters { get; set; } = 24;
    [Export] public int MaximumTargetsEvaluatedPerCandidate { get; set; } = 64;
    [Export] public float EliteScoreBonus { get; set; } = 1.5f;
    [Export] public float BossScoreBonus { get; set; } = 3.0f;
    [Export] public float ExpectedDamageWeight { get; set; } = 0.02f;
    [Export] public float ReactionCountWeight { get; set; } = 1.0f;
    [Export] public float DistancePenaltyWeight { get; set; } = 0.04f;
    [Export] public float HoldChargeMaximumSeconds { get; set; } = 8.0f;
    [Export] public float ForcedTriggerScoreMultiplier { get; set; } = 0.65f;
    [Export] public float CatalysisBaseDamage { get; set; } = 1.5f;
    [Export] public bool DiversityBonusEnabled { get; set; }

    public CatalysisState State { get; private set; } = CatalysisState.Charging;
    public float Charge { get; private set; }
    public float BestScore { get; private set; }
    public int BestReactiveTargetCount { get; private set; }
    public Vector2 BestPosition { get; private set; }
    public int TotalExecutions { get; private set; }
    public int TotalReactionsTriggered { get; private set; }
    public double TotalReactionDamage { get; private set; }
    public float ReadyWaitingSeconds { get; private set; }
    public string LastResultText { get; private set; } = string.Empty;
    public double LastResultRemaining { get; private set; }

    private readonly List<EntityHandle> _queryBuffer = new(64);
    private readonly List<EntityHandle> _innerQueryBuffer = new(64);
    private readonly List<(EntityHandle Handle, Vector2 Position, CatalysisCandidateInput Input)> _candidates = new(32);
    private EnemySystem _enemies = null!;
    private SpatialGrid _grid = null!;
    private CombatSystem _combat = null!;
    private ElementSystem _elements = null!;
    private RunController _run = null!;
    private RunStatistics _statistics = null!;
    private PlayerController _player = null!;
    private float _evaluationRemaining;
    private bool _countingReactions;
    private int _pendingReactionCount;
    private float _pendingReactionDamage;

    public override void _Ready()
    {
        _enemies = GetNode<EnemySystem>("../EntitySystem");
        _grid = GetNode<SpatialGrid>("../SpatialGrid");
        _combat = GetNode<CombatSystem>("../CombatSystem");
        _elements = GetNode<ElementSystem>("../ElementSystem");
        _run = GetNode<RunController>("../RunController");
        _statistics = GetNode<RunStatistics>("../RunStatistics");
        _player = GetNode<PlayerController>("../../WorldRoot/Player");
        _elements.ReactionResolved += OnReactionResolved;
    }

    public override void _ExitTree()
    {
        if (IsInstanceValid(_elements))
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

        FinalizeExecutionWindow();
        float delta = (float)deltaValue;
        LastResultRemaining = Math.Max(0.0, LastResultRemaining - deltaValue);

        if (Charge < 1.0f)
        {
            Charge = Math.Min(1.0f, Charge + delta / Math.Max(0.1f, ChargeDuration));
            State = CatalysisState.Charging;
            if (Charge >= 1.0f)
            {
                _statistics.RecordCatalysisChargeCompleted();
                ChargeCompleted?.Invoke();
                _evaluationRemaining = 0.0f;
            }
            return;
        }

        // Charge is retained until an execution succeeds; evaluation runs on a
        // fixed interval so cost stays bounded regardless of enemy count.
        _evaluationRemaining -= delta;
        if (_evaluationRemaining > 0.0f && State is CatalysisState.Evaluating or CatalysisState.ReadyWaiting)
        {
            return;
        }
        _evaluationRemaining = EvaluationInterval;
        State = CatalysisState.Evaluating;
        EvaluateCandidates();

        if (BestReactiveTargetCount <= 0)
        {
            State = CatalysisState.ReadyWaiting;
            ReadyWaitingSeconds = 0.0f;
            return;
        }

        float requiredScore = MinimumScore;
        if (ReadyWaitingSeconds >= HoldChargeMaximumSeconds)
        {
            requiredScore = MinimumScore * ForcedTriggerScoreMultiplier;
        }

        if (BestScore < requiredScore || BestReactiveTargetCount < MinimumReactiveTargets)
        {
            State = CatalysisState.ReadyWaiting;
            ReadyWaitingSeconds += EvaluationInterval;
            return;
        }

        ExecuteAt(BestPosition);
    }

    public void ApplyOptimization(float chargeDurationMultiplier, float minimumScoreMultiplier)
    {
        ChargeDuration *= chargeDurationMultiplier;
        MinimumScore *= minimumScoreMultiplier;
        DiversityBonusEnabled = true;
    }

    /// <summary>Debug/test hook: skip charging and execute immediately at a position.</summary>
    public void DebugTriggerAt(Vector2 position)
    {
        Charge = 1.0f;
        ExecuteAt(position);
        FinalizeExecutionWindow();
    }

    public void DebugForceChargeFull() => Charge = 1.0f;

    private void EvaluateCandidates()
    {
        Vector2 playerPosition = GetPlayerPosition();
        _candidates.Clear();
        _grid.QueryCircle(playerPosition, SearchRadiusFromPlayer, _queryBuffer);

        foreach (EntityHandle candidate in _queryBuffer)
        {
            if (!_enemies.TryGet(candidate, out EnemyState enemy))
            {
                continue;
            }

            ReactionPreview preview = ElementSystem.TryPreviewCatalysis(enemy.Elements);
            bool isBoss = enemy.Archetype == EnemyArchetype.BossAshenColossus;
            if (!preview.CanReact && !isBoss)
            {
                continue;
            }

            BuildCandidateInput(candidate, enemy, playerPosition, out CatalysisCandidateInput input);
            _candidates.Add((candidate, enemy.Position, input));
            if (_candidates.Count >= MaximumCandidateCenters)
            {
                break;
            }
        }

        _candidates.Sort((left, right) =>
        {
            int order = left.Input.DistanceToPlayer.CompareTo(right.Input.DistanceToPlayer);
            return order != 0 ? order : left.Handle.Index.CompareTo(right.Handle.Index);
        });
        if (_candidates.Count > MaximumCandidateCenters)
        {
            _candidates.RemoveRange(MaximumCandidateCenters, _candidates.Count - MaximumCandidateCenters);
        }

        Vector2 bestPosition = default;
        CatalysisCandidateInput bestInput = default;
        float bestScore = float.NegativeInfinity;
        bool hasBest = false;
        foreach ((_, Vector2 position, CatalysisCandidateInput input) in _candidates)
        {
            float score = CatalysisScoring.Evaluate(
                input, ReactionCountWeight, ExpectedDamageWeight,
                EliteScoreBonus, BossScoreBonus, DistancePenaltyWeight,
                DiversityBonusEnabled);
            if (!hasBest || CatalysisScoring.PreferLeft(input, score, bestInput, bestScore))
            {
                hasBest = true;
                bestScore = score;
                bestInput = input;
                bestPosition = position;
            }
        }

        if (!hasBest)
        {
            BestScore = 0.0f;
            BestReactiveTargetCount = 0;
            return;
        }
        BestScore = bestScore;
        BestReactiveTargetCount = bestInput.ReactiveTargetCount;
        BestPosition = bestPosition;
    }

    private void BuildCandidateInput(
        EntityHandle handle,
        in EnemyState center,
        Vector2 playerPosition,
        out CatalysisCandidateInput input)
    {
        int reactiveTargets = 0;
        float expectedDamage = 0.0f;
        int eliteCount = 0;
        int bossCount = 0;
        long reactionKindMask = 0;
        int distinctKinds = 0;

        // Separate buffer: the outer candidate loop is still enumerating.
        _grid.QueryCircle(center.Position, EffectRadius, _innerQueryBuffer);
        int evaluated = 0;
        foreach (EntityHandle target in _innerQueryBuffer)
        {
            if (evaluated >= MaximumTargetsEvaluatedPerCandidate)
            {
                break;
            }
            evaluated++;
            if (!_enemies.TryGet(target, out EnemyState enemy))
            {
                continue;
            }
            if (enemy.IsElite)
            {
                eliteCount++;
            }
            if (enemy.Archetype == EnemyArchetype.BossAshenColossus)
            {
                bossCount++;
            }
            ReactionPreview preview = ElementSystem.TryPreviewCatalysis(enemy.Elements);
            if (!preview.CanReact)
            {
                continue;
            }
            reactiveTargets++;
            expectedDamage += preview.ExpectedDamage;
            long kindBit = 1L << (int)preview.Reaction;
            if ((reactionKindMask & kindBit) == 0)
            {
                reactionKindMask |= kindBit;
                distinctKinds++;
            }
        }

        input = new CatalysisCandidateInput(
            reactiveTargets,
            expectedDamage,
            eliteCount,
            bossCount,
            center.Position.DistanceTo(playerPosition),
            distinctKinds,
            (uint)handle.Index);
    }

    private void ExecuteAt(Vector2 position)
    {
        State = CatalysisState.Executing;
        _grid.QueryCircle(position, EffectRadius, _queryBuffer);
        if (_queryBuffer.Count == 0)
        {
            // All candidates died before execution: keep the charge, never
            // detonate empty ground.
            State = CatalysisState.ReadyWaiting;
            return;
        }

        // Sorted submissions keep execution order deterministic.
        _queryBuffer.Sort((left, right) => left.Index.CompareTo(right.Index));
        _countingReactions = true;
        _pendingReactionCount = 0;
        _pendingReactionDamage = 0.0f;
        foreach (EntityHandle target in _queryBuffer)
        {
            if (!_enemies.TryGet(target, out _))
            {
                continue;
            }
            _combat.Submit(new DamageContext(
                target,
                "protocol.auto_catalysis",
                CatalysisBaseDamage,
                ElementType.None,
                0,
                DamageFlags.ForceReactionCheck));
        }
    }

    private void FinalizeExecutionWindow()
    {
        if (!_countingReactions)
        {
            return;
        }
        _countingReactions = false;
        if (_pendingReactionCount <= 0)
        {
            // No reaction resolved: the charge is not consumed.
            State = CatalysisState.Charging;
            Charge = Math.Max(Charge, 1.0f);
            return;
        }

        Charge = 0.0f;
        State = CatalysisState.Charging;
        TotalExecutions++;
        TotalReactionsTriggered += _pendingReactionCount;
        TotalReactionDamage += _pendingReactionDamage;
        _statistics.RecordCatalysisExecution(_pendingReactionCount, _pendingReactionDamage);
        LastResultText = $"自动催化完成 · 触发 {_pendingReactionCount} 次反应 · 造成 {_pendingReactionDamage:0} 反应伤害";
        LastResultRemaining = 1.5;
        Vector2 position = BestPosition;
        Executed?.Invoke(position, _pendingReactionCount, _pendingReactionDamage);
    }

    private void OnReactionResolved(EntityHandle target, ReactionKind kind, float damage)
    {
        if (_countingReactions)
        {
            _pendingReactionCount++;
            _pendingReactionDamage += Math.Max(0.0f, damage);
        }
    }

    private Vector2 GetPlayerPosition() => new(_player.GlobalPosition.X, _player.GlobalPosition.Z);
}
