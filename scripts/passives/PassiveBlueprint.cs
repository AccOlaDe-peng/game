using Catalyst.Elements;

namespace Catalyst.Passives;

/// <summary>
/// Plain-C# passive description. Godot-free so catalogs, HUD text, save data
/// and unit tests can reference passives without touching native types.
/// </summary>
public sealed class PassiveBlueprint
{
    public string Id { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string IconPath { get; init; } = string.Empty;
    public PassiveTriggerKind Trigger { get; init; }
    public PassiveConditionKind Condition { get; init; }
    public PassiveEffectKind Effect { get; init; }
    public string TargetWeaponId { get; init; } = string.Empty;
    public ElementType RequiredElement { get; init; }
    public float CooldownSeconds { get; init; }
    public float IntervalSeconds { get; init; } = 1.0f;
    public float Radius { get; init; }
    public float Value { get; init; }
    public float SecondaryValue { get; init; }
    public int Count { get; init; }
    public int Priority { get; init; }
    public int MaximumTriggersPerSecond { get; init; } = 10;
    public bool AllowSelfTriggeredEvents { get; init; }
    public bool EnabledByDefault { get; init; } = true;

    /// <summary>Creates the editor-facing resource form (Godot runtime only).</summary>
    public PassiveDefinition ToDefinition() => new()
    {
        Id = new Godot.StringName(Id),
        DisplayName = DisplayName,
        Description = Description,
        IconPath = IconPath,
        Trigger = Trigger,
        Condition = Condition,
        Effect = Effect,
        TargetWeaponId = new Godot.StringName(TargetWeaponId),
        CooldownSeconds = CooldownSeconds,
        IntervalSeconds = IntervalSeconds,
        Radius = Radius,
        Value = Value,
        SecondaryValue = SecondaryValue,
        Count = Count,
        Priority = Priority,
        MaximumTriggersPerSecond = MaximumTriggersPerSecond,
        AllowSelfTriggeredEvents = AllowSelfTriggeredEvents,
        EnabledByDefault = EnabledByDefault
    };
}
