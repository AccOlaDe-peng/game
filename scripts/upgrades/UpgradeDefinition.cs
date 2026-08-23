using Catalyst.Core;
using Godot;

namespace Catalyst.Upgrades;

[GlobalClass]
public partial class UpgradeDefinition : ContentDefinition
{
    [Export(PropertyHint.MultilineText)]
    public string Description { get; set; } = string.Empty;

    [Export]
    public UpgradeEffect Effect { get; set; }

    [Export]
    public float Multiplier { get; set; } = 1.0f;
}
