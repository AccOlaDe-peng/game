using Catalyst.Core;
using Catalyst.Elements;
using Godot;

namespace Catalyst.Passives;

[GlobalClass]
public partial class PassiveDefinition : ContentDefinition
{
    [Export] public string Description { get; set; } = string.Empty;
    [Export] public string IconPath { get; set; } = string.Empty;
    [Export] public PassiveTriggerKind Trigger { get; set; }
    [Export] public PassiveConditionKind Condition { get; set; }
    [Export] public PassiveEffectKind Effect { get; set; }
    [Export] public StringName TargetWeaponId { get; set; } = new();
    [Export] public StringName RequiredPassiveId { get; set; } = new();
    [Export] public StringName RequiredCardId { get; set; } = new();
    [Export] public ElementType RequiredElement { get; set; }
    [Export] public ReactionKind RequiredReaction { get; set; }
    [Export] public float CooldownSeconds { get; set; }
    [Export] public float IntervalSeconds { get; set; } = 1.0f;
    [Export] public float Radius { get; set; }
    [Export] public float Value { get; set; }
    [Export] public float SecondaryValue { get; set; }
    [Export] public int Count { get; set; }
    [Export] public int Priority { get; set; }
    [Export] public int MaximumTriggersPerSecond { get; set; } = 10;
    [Export] public bool AllowSelfTriggeredEvents { get; set; }
    [Export] public bool EnabledByDefault { get; set; } = true;
}
