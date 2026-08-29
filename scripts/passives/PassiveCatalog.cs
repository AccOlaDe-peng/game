using Catalyst.Meta;

namespace Catalyst.Passives;

/// <summary>
/// Code-defined core passives for the launch characters. Definitions are plain
/// data so runtime, HUD and archive pages all read the same values; the Godot
/// resource form is available through <see cref="PassiveBlueprint.ToDefinition"/>.
/// </summary>
public static class PassiveCatalog
{
    public const string ElementalistCatalysisOptimizer = "passive.elementalist.catalysis_optimizer";
    public const string ElementalistPayloadAdapter = "passive.elementalist.payload_adapter";
    public const string EchoHunterMarkPriority = "passive.echo_hunter.mark_priority";
    public const string EchoHunterAutoRecall = "passive.echo_hunter.auto_recall";
    public const string EchoHunterMarkSpread = "passive.echo_hunter.mark_spread";
    public const string RiftEngineerCapacitySafety = "passive.rift_engineer.capacity_safety";
    public const string RiftEngineerEliteOverload = "passive.rift_engineer.elite_overload";
    public const string RiftEngineerSteadyMine = "passive.rift_engineer.steady_mine";

    public static IReadOnlyList<PassiveBlueprint> CorePassives { get; } = new[]
    {
        Define(ElementalistCatalysisOptimizer, "优化催化协议",
            "催化协议充能速度提高 15%，触发评分要求降低 10%，评分额外偏好区域内的不同反应种类（每种 +0.5，上限 1.5）。",
            PassiveTriggerKind.None, PassiveConditionKind.Always, PassiveEffectKind.None,
            priority: 100),
        Define(ElementalistPayloadAdapter, "载荷适配",
            "本局第一次安装载荷卡时，该卡最终功耗减少 1（最低为 0）。",
            PassiveTriggerKind.LoadoutChanged, PassiveConditionKind.PayloadDiscountAvailable,
            PassiveEffectKind.ApplyPayloadDiscount, priority: 90),
        Define(EchoHunterMarkPriority, "回响追猎",
            "弹射与追踪目标选择时，带标记的目标获得 +1000 固定优先级。",
            PassiveTriggerKind.None, PassiveConditionKind.Always, PassiveEffectKind.None,
            priority: 100),
        Define(EchoHunterAutoRecall, "自动回收",
            "圆盘剩余弹射次数归零或存活时间达到上限 70% 时自动返回，返程获得 1 次额外穿透；每个圆盘只执行一次。",
            PassiveTriggerKind.None, PassiveConditionKind.Always, PassiveEffectKind.RecallProjectile,
            priority: 95),
        Define(EchoHunterMarkSpread, "标记扩散",
            "带标记的敌人死亡时，向 5 米内最近的最多 2 个未标记敌人传播 1 层标记。",
            PassiveTriggerKind.EnemyKilled, PassiveConditionKind.TargetIsMarked,
            PassiveEffectKind.SpreadMark, cooldown: 0.1f, radius: 5.0f, value: 1f, count: 2,
            priority: 40),
        Define(RiftEngineerCapacitySafety, "容量保护",
            "部署物达到上限时，先触发最早创建的合法部署物，再创建新部署物。",
            PassiveTriggerKind.None, PassiveConditionKind.Always, PassiveEffectKind.None,
            priority: 100),
        Define(RiftEngineerEliteOverload, "超载监测",
            "精英或 Boss 处于至少 2 台部署物覆盖范围内时，触发距离最近的部署物，并强化下一台创建的部署物（伤害 ×1.25，范围 ×1.15）。",
            PassiveTriggerKind.Interval, PassiveConditionKind.TargetIsElite,
            PassiveEffectKind.OverloadNearestDeployment, cooldown: 6.0f, interval: 0.5f,
            radius: 12.0f, value: 2f, priority: 50),
        Define(RiftEngineerSteadyMine, "稳态布雷",
            "每 6 秒在朝向最近敌人的方向 3 米处放置一枚弱化地雷（伤害 ×0.45，范围 ×0.8），受部署上限约束。",
            PassiveTriggerKind.Interval, PassiveConditionKind.DeploymentBelowLimit,
            PassiveEffectKind.CreateDeployment, cooldown: 0.0f, interval: 6.0f,
            value: 0.45f, secondaryValue: 0.8f,
            priority: 60)
    };

    private static readonly Dictionary<string, PassiveBlueprint> Definitions = CreateIndex();

    public static PassiveBlueprint Get(string passiveId) => Definitions[passiveId];

    public static IReadOnlyList<PassiveBlueprint> GetMany(IReadOnlyList<string> passiveIds)
    {
        List<PassiveBlueprint> result = new(passiveIds.Count);
        foreach (string passiveId in passiveIds)
        {
            if (Definitions.TryGetValue(passiveId, out PassiveBlueprint? definition))
            {
                result.Add(definition);
            }
        }
        return result;
    }

    private static PassiveBlueprint Define(
        string id,
        string displayName,
        string description,
        PassiveTriggerKind trigger,
        PassiveConditionKind condition,
        PassiveEffectKind effect,
        float cooldown = 0.0f,
        float interval = 1.0f,
        float radius = 0.0f,
        float value = 0.0f,
        float secondaryValue = 0.0f,
        int count = 0,
        int priority = 0)
    {
        return new PassiveBlueprint
        {
            Id = id,
            DisplayName = displayName,
            Description = description,
            Trigger = trigger,
            Condition = condition,
            Effect = effect,
            CooldownSeconds = cooldown,
            IntervalSeconds = interval,
            Radius = radius,
            Value = value,
            SecondaryValue = secondaryValue,
            Count = count,
            Priority = priority
        };
    }

    private static Dictionary<string, PassiveBlueprint> CreateIndex()
    {
        Dictionary<string, PassiveBlueprint> index = new(StringComparer.Ordinal);
        foreach (PassiveBlueprint definition in CorePassives)
        {
            index[definition.Id] = definition;
        }
        return index;
    }
}

public static class CharacterPassiveIds
{
    public static IReadOnlyList<string> For(string characterId) => characterId switch
    {
        CharacterCatalog.EchoHunterId => new[]
        {
            PassiveCatalog.EchoHunterMarkPriority,
            PassiveCatalog.EchoHunterAutoRecall,
            PassiveCatalog.EchoHunterMarkSpread
        },
        CharacterCatalog.RiftEngineerId => new[]
        {
            PassiveCatalog.RiftEngineerCapacitySafety,
            PassiveCatalog.RiftEngineerEliteOverload,
            PassiveCatalog.RiftEngineerSteadyMine
        },
        _ => new[]
        {
            PassiveCatalog.ElementalistCatalysisOptimizer,
            PassiveCatalog.ElementalistPayloadAdapter
        }
    };
}
