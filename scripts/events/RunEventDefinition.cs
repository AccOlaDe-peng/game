using Catalyst.Core;
using Godot;

namespace Catalyst.Events;

[GlobalClass]
public partial class RunEventDefinition : ContentDefinition
{
    [Export] public RunEventKind Kind { get; set; }
    [Export] public float Radius { get; set; } = 4.0f;
    [Export] public float RequiredProgressSeconds { get; set; } = 10.0f;
    [Export] public float TimeLimitSeconds { get; set; } = 45.0f;
    [Export] public float LeaveDecayPerSecond { get; set; } = 0.25f;
    [Export] public int ExperienceReward { get; set; } = 10;
    [Export] public float ActivePressureMultiplier { get; set; } = 1.2f;
}
