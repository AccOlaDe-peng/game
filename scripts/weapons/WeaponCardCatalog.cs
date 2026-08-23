using Catalyst.Spells;

namespace Catalyst.Weapons;

public static class WeaponCardCatalog
{
    private static readonly WeaponCardDefinition[] Definitions =
    {
        new("card.pulse.accelerated_barrel", "加速枪管", "弹速提高 18%", SpellCastKind.ArcaneMissile, WeaponCardEffect.ProjectileSpeed, 0.18f),
        new("card.pulse.focused_rifling", "聚焦膛线", "伤害提高 16%", SpellCastKind.ArcaneMissile, WeaponCardEffect.Damage, 0.16f),
        new("card.pulse.overheat_magazine", "过热弹仓", "伤害提高 10%，作为连续射击体系基础", SpellCastKind.ArcaneMissile, WeaponCardEffect.Damage, 0.10f),
        new("card.pulse.prism_muzzle", "棱镜枪口", "每级额外发射 1 枚弱化脉冲", SpellCastKind.ArcaneMissile, WeaponCardEffect.ProjectileCount, 1.0f, 2),
        new("card.pulse.rail_pierce", "轨道贯穿", "每级额外穿透 1 个目标", SpellCastKind.ArcaneMissile, WeaponCardEffect.MaximumHits, 1.0f),
        new("card.pulse.fast_transduction", "快速换能", "射击间隔缩短 8%", SpellCastKind.ArcaneMissile, WeaponCardEffect.Cooldown, 0.08f),

        new("card.disc.sharpened_edge", "锐化边缘", "圆盘伤害提高 14%", SpellCastKind.RicochetDisc, WeaponCardEffect.Damage, 0.14f),
        new("card.disc.echo_trail", "回声轨迹", "圆盘存续时间提高 20%", SpellCastKind.RicochetDisc, WeaponCardEffect.Lifetime, 0.20f),
        new("card.disc.twin_disc", "双生圆盘", "每级额外投出 1 枚圆盘", SpellCastKind.RicochetDisc, WeaponCardEffect.ProjectileCount, 1.0f, 2),
        new("card.disc.prey_memory", "猎物记忆", "圆盘伤害提高 12%", SpellCastKind.RicochetDisc, WeaponCardEffect.Damage, 0.12f),
        new("card.disc.perfect_recall", "完美回收", "投掷间隔缩短 9%", SpellCastKind.RicochetDisc, WeaponCardEffect.Cooldown, 0.09f),
        new("card.disc.mark_transfer", "标记转移", "每级增加 1 次弹射", SpellCastKind.RicochetDisc, WeaponCardEffect.BounceCount, 1.0f),

        new("card.mine.cluster_charge", "集束装药", "爆炸范围提高 14%", SpellCastKind.ElementMine, WeaponCardEffect.ExplosionRadius, 0.14f),
        new("card.mine.delayed_boost", "延时增压", "地雷伤害提高 18%", SpellCastKind.ElementMine, WeaponCardEffect.Damage, 0.18f),
        new("card.mine.proximity_trigger", "感应引爆", "部署间隔缩短 12%", SpellCastKind.ElementMine, WeaponCardEffect.Cooldown, 0.12f),
        new("card.mine.chain_field", "连锁雷区", "爆炸范围提高 18%", SpellCastKind.ElementMine, WeaponCardEffect.ExplosionRadius, 0.18f),
        new("card.mine.residual_medium", "残留介质", "地雷伤害提高 15%", SpellCastKind.ElementMine, WeaponCardEffect.Damage, 0.15f),
        new("card.mine.portable_workshop", "便携工坊", "部署间隔缩短 10%", SpellCastKind.ElementMine, WeaponCardEffect.Cooldown, 0.10f)
    };

    public static IReadOnlyList<WeaponCardDefinition> All => Definitions;
    public static IEnumerable<WeaponCardDefinition> ForWeapon(SpellCastKind weapon) =>
        Definitions.Where(card => card.Weapon == weapon);
    public static WeaponCardDefinition Get(string id) =>
        Definitions.First(card => card.Id == id);
}
