using Catalyst.Spells;

namespace Catalyst.Weapons;

public sealed record WeaponCardDefinition(
    string Id,
    string DisplayName,
    string Description,
    SpellCastKind Weapon,
    WeaponCardEffect Effect,
    float Value,
    int MaximumLevel = 3,
    int PowerCost = 1);
