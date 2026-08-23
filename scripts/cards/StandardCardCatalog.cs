using Catalyst.Spells;

namespace Catalyst.Cards;

public static class StandardCardCatalog
{
    private static readonly SpellCastKind[] ProjectileWeapons =
    {
        SpellCastKind.ArcaneMissile, SpellCastKind.Fireball, SpellCastKind.FrostLance,
        SpellCastKind.RicochetDisc
    };
    private static readonly SpellCastKind[] AllWeapons = Enum.GetValues<SpellCastKind>();

    private static StandardCardDefinition Card(string id, string name, string description,
        StandardCardCategory category, StandardCardEffect effect, int power, int maxLevel,
        IEnumerable<SpellCastKind>? weapons = null) =>
        new(id, name, description, category, effect, power, maxLevel,
            new HashSet<SpellCastKind>(weapons ?? AllWeapons));

    private static readonly StandardCardDefinition[] Definitions =
    {
        Card("card.behavior.pierce", "穿透", "穿透 +2", StandardCardCategory.Behavior, StandardCardEffect.Pierce, 2, 3, ProjectileWeapons),
        Card("card.behavior.split", "分裂", "首次命中生成 2 枚弱化子弹", StandardCardCategory.Behavior, StandardCardEffect.Split, 3, 1, ProjectileWeapons),
        Card("card.behavior.bounce", "弹射", "弹射 +2", StandardCardCategory.Behavior, StandardCardEffect.Bounce, 2, 3, ProjectileWeapons),
        Card("card.behavior.multishot", "多重射击", "弹数 +2，小角度扇形", StandardCardCategory.Behavior, StandardCardEffect.Multishot, 2, 2, ProjectileWeapons),
        Card("card.behavior.homing", "追踪", "优先追猎标记目标", StandardCardCategory.Behavior, StandardCardEffect.Homing, 3, 1, ProjectileWeapons),
        Card("card.behavior.boomerang", "回旋", "次数耗尽后返程并获得穿透", StandardCardCategory.Behavior, StandardCardEffect.Boomerang, 2, 1, ProjectileWeapons),

        Card("card.payload.fire", "火载荷", "安装火基元", StandardCardCategory.Payload, StandardCardEffect.FirePayload, 2, 1),
        Card("card.payload.water", "水载荷", "安装水基元", StandardCardCategory.Payload, StandardCardEffect.WaterPayload, 2, 1),
        Card("card.payload.wind", "风载荷", "安装风基元", StandardCardCategory.Payload, StandardCardEffect.WindPayload, 2, 1),
        Card("card.payload.earth", "土载荷", "安装土基元", StandardCardCategory.Payload, StandardCardEffect.EarthPayload, 2, 1),
        Card("card.payload.lightning", "雷载荷", "安装雷基元", StandardCardCategory.Payload, StandardCardEffect.LightningPayload, 2, 1),
        Card("card.payload.mark", "标记载荷", "安装中性标记，不参与融合", StandardCardCategory.Payload, StandardCardEffect.MarkPayload, 1, 1),

        Card("card.action.explosion", "爆炸", "命中产生范围动作", StandardCardCategory.Action, StandardCardEffect.Explosion, 2, 3),
        Card("card.action.fragments", "碎片", "动作生成弱化碎片", StandardCardCategory.Action, StandardCardEffect.Fragments, 2, 2),
        Card("card.action.nova", "新星", "以触发点释放环形动作", StandardCardCategory.Action, StandardCardEffect.Nova, 2, 2),
        Card("card.action.projectile_copy", "弹体复制", "复制当前武器弹体", StandardCardCategory.Action, StandardCardEffect.ProjectileCopy, 3, 2, ProjectileWeapons),
        Card("card.action.ground_field", "地面领域", "留下短时领域", StandardCardCategory.Action, StandardCardEffect.GroundField, 3, 2),
        Card("card.action.status_spread", "状态传播", "向附近目标传播状态", StandardCardCategory.Action, StandardCardEffect.StatusSpread, 3, 2),

        Card("card.trigger.on_pierce", "穿透时", "穿透后执行已接线动作", StandardCardCategory.Trigger, StandardCardEffect.OnPierce, 1, 1, ProjectileWeapons),
        Card("card.trigger.on_bounce", "弹射时", "弹射后执行已接线动作", StandardCardCategory.Trigger, StandardCardEffect.OnBounce, 1, 1, ProjectileWeapons),
        Card("card.trigger.on_freeze", "冻结时", "冻结目标时执行已接线动作", StandardCardCategory.Trigger, StandardCardEffect.OnFreeze, 1, 1),
        Card("card.trigger.on_dash", "冲刺时", "冲刺结束后执行已接线动作", StandardCardCategory.Trigger, StandardCardEffect.OnDash, 1, 1, new[] { SpellCastKind.ElementMine }),

        Card("card.reaction.shatter", "碎冰", "冻结目标受击时改写为碎片动作", StandardCardCategory.Reaction, StandardCardEffect.Shatter, 3, 1),
        Card("card.reaction.burn_propagation", "燃爆传播", "燃烧目标死亡时改写为传播动作", StandardCardCategory.Reaction, StandardCardEffect.BurnPropagation, 3, 1),
        Card("card.reaction.critical_echo", "暴击回响", "暴击时改写为弹体复制", StandardCardCategory.Reaction, StandardCardEffect.CriticalEcho, 3, 1, ProjectileWeapons),
        Card("card.reaction.death_nova", "死亡新星", "目标死亡时改写为新星动作", StandardCardCategory.Reaction, StandardCardEffect.DeathNova, 3, 1)
    };

    public static IReadOnlyList<StandardCardDefinition> All => Definitions;
    public static IEnumerable<StandardCardDefinition> ForWeapon(SpellCastKind weapon) =>
        Definitions.Where(card => card.IsCompatible(weapon));
    public static StandardCardDefinition Get(string id) => Definitions.First(card => card.Id == id);
}
