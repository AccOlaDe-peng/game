namespace Catalyst.Elements;

public sealed class ElementResolver
{
    public const int MaximumStacks = 6;
    public const float StatusDuration = 6.0f;
    public const float BurnDamagePerStackPerSecond = 0.7f;

    private readonly ReactionRule[] _rules;

    public ElementResolver(IEnumerable<ReactionRule> rules)
    {
        _rules = rules
            .OrderByDescending(rule => rule.Priority)
            .ThenBy(rule => rule.Id, StringComparer.Ordinal)
            .ToArray();
    }

    public bool ApplyElement(
        ref ElementRuntimeState runtime,
        ElementType element,
        int stacks,
        bool canTriggerReaction,
        out ReactionResult reaction)
    {
        reaction = default;
        if (element == ElementType.None || stacks <= 0)
        {
            return false;
        }

        ref ElementState state = ref GetState(ref runtime, element);
        state.Stacks = Math.Min(MaximumStacks, state.Stacks + stacks);
        state.RemainingDuration = GetStatusDuration(element);

        if (element == ElementType.Frost && state.Stacks >= MaximumStacks)
        {
            runtime.FrozenRemaining = Math.Max(runtime.FrozenRemaining, 0.65f);
        }
        else if (element == ElementType.Lightning)
        {
            runtime.StaggerRemaining = Math.Max(runtime.StaggerRemaining, 0.12f);
        }

        return canTriggerReaction && TryResolve(ref runtime, false, out reaction);
    }

    public bool TryCatalyze(ref ElementRuntimeState runtime, out ReactionResult reaction) =>
        TryResolve(ref runtime, true, out reaction);

    public float Tick(ref ElementRuntimeState runtime, float delta)
    {
        float burnDamage = runtime.Fire.Stacks * BurnDamagePerStackPerSecond * delta;
        TickState(ref runtime.Fire, delta);
        TickState(ref runtime.Frost, delta);
        TickState(ref runtime.Lightning, delta);
        TickState(ref runtime.Water, delta);
        TickState(ref runtime.Wind, delta);
        TickState(ref runtime.Earth, delta);
        TickState(ref runtime.Mark, delta);
        runtime.ThermalCooldown = Math.Max(0.0f, runtime.ThermalCooldown - delta);
        runtime.PlasmaCooldown = Math.Max(0.0f, runtime.PlasmaCooldown - delta);
        runtime.ConductiveCooldown = Math.Max(0.0f, runtime.ConductiveCooldown - delta);
        runtime.FrozenRemaining = Math.Max(0.0f, runtime.FrozenRemaining - delta);
        runtime.StaggerRemaining = Math.Max(0.0f, runtime.StaggerRemaining - delta);
        runtime.SteamScaldRemaining = Math.Max(0.0f, runtime.SteamScaldRemaining - delta);
        burnDamage += runtime.SteamScaldRemaining > 0.0f ? 1.1f * delta : 0.0f;
        return burnDamage;
    }

    public static float GetMovementMultiplier(in ElementRuntimeState runtime)
    {
        if (runtime.FrozenRemaining > 0.0f || runtime.StaggerRemaining > 0.0f)
        {
            return 0.0f;
        }

        return Math.Max(0.45f, 1.0f - runtime.Frost.Stacks * 0.08f);
    }

    public static int GetStacks(in ElementRuntimeState runtime, ElementType element) => element switch
    {
        ElementType.Fire => runtime.Fire.Stacks,
        ElementType.Frost => runtime.Frost.Stacks,
        ElementType.Lightning => runtime.Lightning.Stacks,
        ElementType.Water => runtime.Water.Stacks,
        ElementType.Wind => runtime.Wind.Stacks,
        ElementType.Earth => runtime.Earth.Stacks,
        ElementType.Mark => runtime.Mark.Stacks,
        _ => 0
    };

    private bool TryResolve(
        ref ElementRuntimeState runtime,
        bool catalyzed,
        out ReactionResult reaction)
    {
        foreach (ReactionRule rule in _rules)
        {
            int threshold = catalyzed ? rule.CatalyzeThreshold : rule.AutomaticThreshold;
            ref ElementState first = ref GetState(ref runtime, rule.First);
            ref ElementState second = ref GetState(ref runtime, rule.Second);
            if (first.Stacks < threshold || second.Stacks < threshold ||
                GetCooldown(runtime, rule.Kind) > 0.0f)
            {
                continue;
            }

            int firstConsumed = Math.Min(rule.StacksConsumed, first.Stacks);
            int secondConsumed = Math.Min(rule.StacksConsumed, second.Stacks);
            first.Stacks -= firstConsumed;
            second.Stacks -= secondConsumed;
            if (first.Stacks == 0)
            {
                first.RemainingDuration = 0.0f;
            }
            if (second.Stacks == 0)
            {
                second.RemainingDuration = 0.0f;
            }

            SetCooldown(ref runtime, rule.Kind, rule.InternalCooldown);
            float damage = rule.BaseDamage +
                (firstConsumed + secondConsumed) * rule.DamagePerConsumedStack;
            reaction = new ReactionResult(rule, firstConsumed, secondConsumed, damage);
            return true;
        }

        reaction = default;
        return false;
    }

    private static void TickState(ref ElementState state, float delta)
    {
        if (state.Stacks <= 0)
        {
            return;
        }

        state.RemainingDuration -= delta;
        if (state.RemainingDuration <= 0.0f)
        {
            state = default;
        }
    }

    private static ref ElementState GetState(ref ElementRuntimeState runtime, ElementType element)
    {
        switch (element)
        {
            case ElementType.Fire:
                return ref runtime.Fire;
            case ElementType.Frost:
                return ref runtime.Frost;
            case ElementType.Lightning:
                return ref runtime.Lightning;
            case ElementType.Water:
                return ref runtime.Water;
            case ElementType.Wind:
                return ref runtime.Wind;
            case ElementType.Earth:
                return ref runtime.Earth;
            case ElementType.Mark:
                return ref runtime.Mark;
            default:
                throw new ArgumentOutOfRangeException(nameof(element), element, null);
        }
    }

    public static float GetStatusDuration(ElementType element) => element switch
    {
        ElementType.Fire => 2.2f,
        ElementType.Water => 2.0f,
        ElementType.Wind => 1.2f,
        ElementType.Earth => 1.6f,
        ElementType.Lightning => 1.0f,
        ElementType.Mark => 3.0f,
        ElementType.Frost => StatusDuration,
        _ => 0.0f
    };

    private static float GetCooldown(in ElementRuntimeState runtime, ReactionKind kind) => 0.0f;

    private static void SetCooldown(ref ElementRuntimeState runtime, ReactionKind kind, float value)
    {
        // Formal V2 reactions are resolved by ElementSystem from pre-hit states.
    }
}
