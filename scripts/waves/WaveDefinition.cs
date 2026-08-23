using Catalyst.Core;
using Godot;

namespace Catalyst.Waves;

[GlobalClass]
public partial class WaveDefinition : ContentDefinition
{
    [Export] public float StartsAtSeconds { get; set; }
    [Export] public float SpawnInterval { get; set; } = 0.5f;
    [Export] public int BatchSize { get; set; } = 1;
    [Export] public float ThreatMultiplier { get; set; } = 1.0f;
}
