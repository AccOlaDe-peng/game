using Catalyst.Spells;

namespace Catalyst.Cards;

public sealed record StandardCardDefinition(
    string Id,
    string DisplayName,
    string Description,
    StandardCardCategory Category,
    StandardCardEffect Effect,
    int PowerCost,
    int MaximumLevel,
    IReadOnlySet<SpellCastKind> CompatibleWeapons)
{
    public bool IsCompatible(SpellCastKind weapon) => CompatibleWeapons.Contains(weapon);
}
