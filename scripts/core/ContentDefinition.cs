using Godot;

namespace Catalyst.Core;

[GlobalClass]
public partial class ContentDefinition : Resource
{
    [Export]
    public StringName Id { get; set; } = new(string.Empty);

    [Export]
    public string DisplayName { get; set; } = string.Empty;
}
