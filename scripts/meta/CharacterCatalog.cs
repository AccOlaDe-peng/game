using Catalyst.Spells;

namespace Catalyst.Meta;

public static class CharacterCatalog
{
    public const string ElementalistId = "character.elementalist";
    public const string EchoHunterId = "character.echo_hunter";
    public const string RiftEngineerId = "character.rift_engineer";
    public const string PulseRifleId = "weapon.pulse_rifle";
    public const string RicochetDiscId = "weapon.ricochet_disc";
    public const string ElementMineId = "weapon.element_mine";

    private static readonly CharacterDefinition[] Definitions =
    {
        new(ElementalistId, "元素行者", "直线投射 · 入门",
            "用稳定的高速射击建立元素组合，适合熟悉反应系统。",
            "元素催化：提前结算范围内满足条件的反应。", "首张载荷卡功耗降低 1。",
            PulseRifleId, "脉冲步枪", SpellCastKind.ArcaneMissile, "初始角色"),
        new(EchoHunterId, "回响猎手", "弹射标记 · 追猎",
            "让攻击在敌群间跳跃，依靠标记和重复命中扩大收益。",
            "回收：召回圆盘，返程获得额外穿透。", "弹射优先选择标记目标。",
            RicochetDiscId, "弹射圆盘", SpellCastKind.RicochetDisc, "完成 1 次对局后解锁"),
        new(RiftEngineerId, "裂隙工匠", "部署爆破 · 控场",
            "布置延迟爆破装置，以领域和连锁引爆控制战场。",
            "超载部署：引爆最近部署物并放置强化装置。", "冲刺终点放置一枚弱化地雷。",
            ElementMineId, "元素地雷", SpellCastKind.ElementMine, "首次击败 Boss 后解锁")
    };

    public static IReadOnlyList<CharacterDefinition> All => Definitions;
    public static CharacterDefinition Default => Definitions[0];
    public static CharacterDefinition Get(string? id) =>
        Definitions.FirstOrDefault(item => item.Id == id) ?? Default;

    public static IReadOnlyList<SpellCastKind> CompatibleWeapons(string? characterId) =>
        characterId switch
        {
            EchoHunterId => new[] { SpellCastKind.RicochetDisc, SpellCastKind.ChainLightning },
            RiftEngineerId => new[] { SpellCastKind.ElementMine, SpellCastKind.OrbitOrb },
            _ => new[] { SpellCastKind.ArcaneMissile, SpellCastKind.Fireball, SpellCastKind.FrostLance }
        };

    public static bool IsCompatibleWeapon(string? characterId, SpellCastKind weapon) =>
        CompatibleWeapons(characterId).Contains(weapon);
}
