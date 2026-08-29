using Catalyst.Cards;
using Catalyst.Elements;
using Catalyst.Spells;
using Catalyst.Weapons;

namespace Catalyst.App;

/// <summary>Godot-free view of an installed weapon, testable without resources.</summary>
public interface IWeaponBuildView
{
    string WeaponDefinitionId { get; }
    int WeaponLevel { get; }
    SpellBranch SelectedBranch { get; }
    int CurrentPower { get; }
    int PowerCapacity { get; }
    IReadOnlyDictionary<string, int> InstalledCards { get; }
    IReadOnlyDictionary<string, int> InstalledStandardCards { get; }
}

public sealed class RunBuildSnapshot
{
    public string CharacterId { get; set; } = string.Empty;
    public string StartingWeaponId { get; set; } = string.Empty;
    public List<string> CharacterPassiveIds { get; set; } = new();
    public List<WeaponBuildSnapshot> Weapons { get; set; } = new();
}

public sealed class WeaponBuildSnapshot
{
    public string WeaponId { get; set; } = string.Empty;
    public int Level { get; set; }
    public string BranchId { get; set; } = string.Empty;
    public int PowerUsed { get; set; }
    public int PowerCapacity { get; set; }
    public Dictionary<string, int> WeaponCards { get; set; } = new();
    public Dictionary<string, int> StandardCards { get; set; } = new();
    public List<CardConnectionSnapshot> Connections { get; set; } = new();
}

public sealed class CardConnectionSnapshot
{
    public string TriggerCardId { get; set; } = string.Empty;
    public string ActionCardId { get; set; } = string.Empty;
    public int Priority { get; set; }
}

/// <summary>Builds a structured run build snapshot from the live spell loadout.</summary>
public static class RunBuildSnapshotBuilder
{
    public static RunBuildSnapshot Build(
        string characterId,
        string startingWeaponId,
        IReadOnlyList<string> characterPassiveIds,
        IReadOnlyList<IWeaponBuildView> loadout)
    {
        RunBuildSnapshot snapshot = new()
        {
            CharacterId = characterId,
            StartingWeaponId = startingWeaponId,
            CharacterPassiveIds = characterPassiveIds.ToList()
        };
        foreach (IWeaponBuildView weapon in loadout)
        {
            snapshot.Weapons.Add(BuildWeapon(weapon));
        }
        return snapshot;
    }

    public static WeaponBuildSnapshot BuildWeapon(IWeaponBuildView weapon)
    {
        Dictionary<string, int> standardCards = weapon.InstalledStandardCards.ToDictionary(
            pair => pair.Key, pair => pair.Value);
        return new WeaponBuildSnapshot
        {
            WeaponId = weapon.WeaponDefinitionId,
            Level = weapon.WeaponLevel,
            BranchId = weapon.SelectedBranch.ToString(),
            PowerUsed = weapon.CurrentPower,
            PowerCapacity = weapon.PowerCapacity,
            WeaponCards = weapon.InstalledCards.ToDictionary(pair => pair.Key, pair => pair.Value),
            StandardCards = standardCards,
            Connections = BuildConnections(standardCards)
        };
    }

    /// <summary>
    /// Trigger/action wiring is derived from installed standard cards: every
    /// installed action card pairs with the first installed trigger card of
    /// the same weapon (one primary action slot per trigger).
    /// </summary>
    public static List<CardConnectionSnapshot> BuildConnections(
        Dictionary<string, int> standardCards)
    {
        List<CardConnectionSnapshot> connections = new();
        List<string> triggers = standardCards.Keys
            .Where(id => StandardCardCatalog.Get(id).Category == StandardCardCategory.Trigger)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        List<string> actions = standardCards.Keys
            .Where(id => StandardCardCatalog.Get(id).Category == StandardCardCategory.Action)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        for (int index = 0; index < Math.Min(triggers.Count, actions.Count); index++)
        {
            connections.Add(new CardConnectionSnapshot
            {
                TriggerCardId = triggers[index],
                ActionCardId = actions[index],
                Priority = actions.Count - index
            });
        }
        return connections;
    }

    public static string Describe(RunBuildSnapshot snapshot)
    {
        if (snapshot.Weapons.Count == 0)
        {
            return "  暂无武器";
        }
        List<string> lines = new();
        foreach (WeaponBuildSnapshot weapon in snapshot.Weapons)
        {
            string branch = weapon.BranchId is "None" or "" ? string.Empty : $" / 分支 {weapon.BranchId}";
            lines.Add($"  {WeaponDisplayName(weapon.WeaponId)} Lv.{weapon.Level}{branch}  功耗 {weapon.PowerUsed}/{weapon.PowerCapacity}");
            foreach ((string cardId, int level) in weapon.WeaponCards.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                lines.Add($"    · {WeaponCardCatalog.Get(cardId).DisplayName} Lv.{level}");
            }
            foreach ((string cardId, int level) in weapon.StandardCards.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                StandardCardDefinition card = StandardCardCatalog.Get(cardId);
                lines.Add($"    · [{CategoryName(card.Category)}] {card.DisplayName} Lv.{level}");
            }
        }
        return string.Join("\n", lines);
    }

    public static string WeaponDisplayName(string weaponId) => weaponId switch
    {
        "spell.arcane_missile" => "奥术飞弹",
        "spell.fireball" => "火球术",
        "spell.frost_lance" => "冰霜长枪",
        "spell.chain_lightning" => "连锁闪电",
        "spell.orbit_orb" => "环绕法球",
        "weapon.ricochet_disc" => "弹射圆盘",
        "weapon.element_mine" => "元素地雷",
        _ => weaponId
    };

    private static string CategoryName(StandardCardCategory category) => category switch
    {
        StandardCardCategory.Behavior => "行为",
        StandardCardCategory.Payload => "载荷",
        StandardCardCategory.Action => "动作",
        StandardCardCategory.Trigger => "触发",
        StandardCardCategory.Reaction => "反应",
        _ => category.ToString()
    };
}
