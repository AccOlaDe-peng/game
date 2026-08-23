using Catalyst.Spells;
using Catalyst.Weapons;
using Catalyst.Cards;

namespace Catalyst.Upgrades;

public enum UpgradeChoiceKind
{
    General,
    AcquireSpell,
    UpgradeSpell,
    InstallWeaponCard,
    InstallStandardCard
}

public sealed record UpgradeChoice(
    string Id,
    string DisplayName,
    string Description,
    float Weight,
    UpgradeChoiceKind Kind,
    UpgradeDefinition? GeneralDefinition = null,
    SpellCastKind Spell = SpellCastKind.ArcaneMissile,
    SpellBranch Branch = SpellBranch.None,
    WeaponCardDefinition? WeaponCard = null,
    StandardCardDefinition? StandardCard = null);
