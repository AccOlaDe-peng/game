# Project Catalyst 纯被动战斗系统设计与开发规范

> 文档状态：功能设计基线 / 待实现  
> 目标版本：M5 及后续版本  
> 适用范围：角色、武器、元素催化、被动触发、升级、HUD、存档、统计与自动化测试  
> 优先级：高  
> 权威声明：本文档覆盖此前所有“玩家主动释放元素催化或角色技能”的设计。若其他文档与本文冲突，以本文为准。

## 1. 设计结论

Project Catalyst 采用纯被动战斗模型。

玩家在局内只执行以下操作：

- 移动。
- 闪避。
- 升级选择。
- 暂停与菜单操作。

所有攻击、元素附着、元素催化、角色特性、部署物、召回和反应衍生效果均由系统自动执行。闪避属于基础移动操作，不属于主动技能。

核心体验定义为：

> 玩家不直接释放技能，而是构筑一套能够自行运转的元素反应系统，再通过走位、聚怪和风险控制为它创造最佳触发条件。

## 2. 设计目标

### 2.1 必须达成

- 玩家不需要技能按键也能形成有深度的战斗决策。
- 自动技能的触发原因和未触发原因必须可理解。
- 三名角色拥有不同的自动战斗逻辑，而不只是不同初始武器。
- 被动能力、标准卡和武器卡共享统一的触发语义。
- 同一 Seed 和同一输入序列得到相同的玩法结果。
- 被动系统在高实体数量下保持有界计算成本。
- 新增被动效果时尽量不修改角色控制器、HUD 或存档核心代码。

### 2.2 不做的内容

- 不增加法术快捷键。
- 不增加需要瞄准后确认的能力。
- 不增加主动引爆、主动召回或主动切换姿态。
- 不使用复杂行为树实现每一发自动攻击。
- 不允许玩家通过高频点击提高输出。
- 不以不可解释的黑箱 AI 替代清晰的触发规则。

## 3. 玩家操作规范

### 3.1 键鼠

| 操作 | 默认输入 | 说明 |
| --- | --- | --- |
| 移动 | WASD | 连续移动 |
| 闪避 | Space | 有冷却的位移动作 |
| 暂停 | Esc | 打开暂停菜单 |
| UI 导航 | 鼠标 / 方向键 | 菜单与升级选择 |
| UI 确认 | 左键 / Enter | 确认选择 |

必须删除鼠标右键“元素催化”绑定及提示。

### 3.2 手柄

| 操作 | 默认输入 | 说明 |
| --- | --- | --- |
| 移动 | 左摇杆 | 连续移动 |
| 闪避 | A / Cross | 有冷却的位移动作 |
| 暂停 | Menu | 打开暂停菜单 |
| UI 导航 | 方向键 / 左摇杆 | 菜单与升级选择 |
| UI 确认 | A / Cross | 确认选择 |

右摇杆、扳机键不承担战斗技能功能。可以保留设备检测，但不得显示无效战斗提示。

## 4. 核心循环

### 4.1 秒级循环

```text
玩家移动与聚怪
→ 自动武器寻找目标并攻击
→ 敌人积累元素基元或状态
→ 被动系统监听命中、状态、反应和位置事件
→ 自动催化系统持续充能并评估候选区域
→ 达到触发阈值后自动催化
→ 反应、击杀、衍生动作继续产生有限事件
→ 玩家通过走位改变下一次自动决策的收益
```

### 4.2 分钟级循环

```text
获得元素余质
→ 升级
→ 选择武器、载荷、动作、触发或反应模块
→ 改变被动系统的触发条件和执行结果
→ 构筑逐渐形成稳定自动循环
```

### 4.3 单局闭环

```text
选择角色与起始武器
→ 进入裂隙
→ 构建自动战斗系统
→ 完成现场目标
→ 击败灾变核心
→ 生成包含完整被动构筑的远征报告
```

## 5. 统一术语

| 术语 | 定义 |
| --- | --- |
| 被动 | 无需玩家技能输入，由规则自动触发的能力 |
| 触发器 | 决定“何时检查”的事件类型 |
| 条件 | 决定当前事件是否允许执行 |
| 效果 | 条件成立后执行的玩法变化 |
| 冷却 | 单个被动两次成功执行之间的最短时间 |
| 内部冷却 | 同一效果防止高频递归触发的独立冷却 |
| 触发预算 | 单帧或单链允许产生的最大衍生效果数量 |
| 催化协议 | 自动评估并触发元素反应的角色装备系统 |
| 候选目标 | 可能成为自动催化中心的敌人或位置 |
| 反应收益 | 催化候选区域的确定性评分 |
| 待触发 | 充能完成，但尚无候选区域达到最低收益 |
| 接线 | 一个触发模块与一个动作模块形成的自动执行关系 |

## 6. 总体技术架构

### 6.1 组件关系

```text
CombatSystem / SpellSystem / ElementSystem / PlayerController
                         │
                         ▼
                 PassiveEventBus
                         │
          ┌──────────────┼──────────────┐
          ▼              ▼              ▼
 CharacterPassiveSystem  AutoCatalysis  CardTriggerSystem
          │              │              │
          └──────────────┼──────────────┘
                         ▼
                 PassiveEffectExecutor
                         │
          ┌──────────────┼──────────────┐
          ▼              ▼              ▼
      DamageQueue   ProjectileSystem  DeploymentSystem
```

### 6.2 架构原则

- 玩法系统只发布语义事件，不直接引用具体角色被动。
- 被动系统不得在事件回调中立即进行无限递归处理。
- 所有衍生效果进入队列，在当前模拟阶段末统一执行。
- 数据定义决定参数与条件，复杂效果由明确的代码处理器执行。
- V1 不实现任意脚本化表达式语言。
- 所有长期存档使用稳定字符串 ID。

## 7. 被动事件总线

### 7.1 组件职责

新增：

```text
scripts/passives/PassiveEventBus.cs
```

职责：

- 接收战斗系统发布的标准事件。
- 按发生顺序写入固定容量队列。
- 在物理帧的被动处理阶段分发事件。
- 为每条事件分配递增序号。
- 携带触发链深度，阻止递归失控。
- 不直接执行伤害、生成投射物或修改状态。

### 7.2 事件枚举

```csharp
public enum PassiveTriggerKind : byte
{
    None = 0,
    Interval = 1,
    DodgeStarted = 2,
    DodgeEnded = 3,
    WeaponCast = 4,
    ProjectileHit = 5,
    ProjectilePierced = 6,
    ProjectileBounced = 7,
    ProjectileReturned = 8,
    ElementApplied = 9,
    StatusApplied = 10,
    EnemyFrozen = 11,
    ReactionTriggered = 12,
    EnemyKilled = 13,
    EliteEnteredRange = 14,
    DeploymentCreated = 15,
    DeploymentLimitReached = 16,
    DeploymentTriggered = 17,
    CatalysisCharged = 18,
    CatalysisExecuted = 19,
    ObjectiveStarted = 20,
    ObjectiveCompleted = 21
}
```

枚举用于运行时分派，不作为长期存档键。存档和资源引用仍使用字符串 ID。

### 7.3 事件上下文字段

```csharp
public readonly record struct PassiveEventContext(
    ulong Sequence,
    PassiveTriggerKind Trigger,
    double SimulationTime,
    EntityHandle Source,
    EntityHandle Target,
    StringName SourceDefinitionId,
    StringName WeaponId,
    StringName EffectId,
    Vector2 Position,
    Vector2 Direction,
    float Value,
    int Count,
    ElementType Element,
    ReactionKind Reaction,
    DamageFlags DamageFlags,
    int ChainDepth,
    ulong RootSequence);
```

字段规则：

- `Sequence`：本次运行中单调递增，不持久化。
- `SimulationTime`：使用局内模拟时间，不使用系统时间。
- `Source`：产生事件的实体；无实体时使用无效句柄。
- `Target`：事件目标；区域事件可为空。
- `SourceDefinitionId`：被动、卡牌或系统来源 ID。
- `WeaponId`：与事件相关的武器；无武器时为空。
- `EffectId`：造成事件的具体效果 ID。
- `Position`：事件世界平面坐标。
- `Direction`：已归一化方向；无方向时为零向量。
- `Value`：伤害、持续时间或距离等主要浮点值。
- `Count`：层数、跳跃次数或目标数量等主要整数值。
- `Element`：相关基元。
- `Reaction`：相关反应。
- `ChainDepth`：衍生事件深度，根事件为 0。
- `RootSequence`：整个触发链的根事件序号。

### 7.4 队列限制

```csharp
public sealed class PassiveEventBudget
{
    public int MaximumEventsPerPhysicsFrame { get; init; } = 2048;
    public int MaximumChainDepth { get; init; } = 4;
    public int MaximumEffectsPerRootEvent { get; init; } = 32;
    public int MaximumTargetsPerEffect { get; init; } = 64;
}
```

超过预算时：

1. 放弃超出的衍生事件。
2. 不影响原始攻击和移动。
3. 开发版本记录一次节流日志。
4. 相同根事件在同一帧只记录一次警告。
5. 调试 HUD 显示被丢弃事件计数。

## 8. 被动定义

### 8.1 文件结构

```text
scripts/passives/
├─ PassiveDefinition.cs
├─ PassiveTriggerKind.cs
├─ PassiveConditionKind.cs
├─ PassiveEffectKind.cs
├─ PassiveEventContext.cs
├─ PassiveEventBus.cs
├─ PassiveRuntime.cs
├─ PassiveSystem.cs
├─ PassiveEffectExecutor.cs
└─ handlers/
   ├─ CatalysisPassiveHandler.cs
   ├─ DiscPassiveHandler.cs
   └─ DeploymentPassiveHandler.cs
```

资源建议放置于：

```text
resources/passives/characters/
resources/passives/cards/
resources/passives/system/
```

### 8.2 PassiveDefinition 字段

```csharp
[GlobalClass]
public partial class PassiveDefinition : ContentDefinition
{
    [Export] public string Description { get; set; } = string.Empty;
    [Export] public string IconPath { get; set; } = string.Empty;
    [Export] public PassiveTriggerKind Trigger { get; set; }
    [Export] public PassiveConditionKind Condition { get; set; }
    [Export] public PassiveEffectKind Effect { get; set; }
    [Export] public StringName TargetWeaponId { get; set; }
    [Export] public StringName RequiredPassiveId { get; set; }
    [Export] public StringName RequiredCardId { get; set; }
    [Export] public ElementType RequiredElement { get; set; }
    [Export] public ReactionKind RequiredReaction { get; set; }
    [Export] public float CooldownSeconds { get; set; }
    [Export] public float Radius { get; set; }
    [Export] public float Value { get; set; }
    [Export] public float SecondaryValue { get; set; }
    [Export] public int Count { get; set; }
    [Export] public int Priority { get; set; }
    [Export] public int MaximumTriggersPerSecond { get; set; } = 10;
    [Export] public bool AllowSelfTriggeredEvents { get; set; }
    [Export] public bool EnabledByDefault { get; set; } = true;
}
```

字段含义：

- `Id`：继承自 `ContentDefinition` 的稳定 ID。
- `DisplayName`：玩家可见名称。
- `Description`：静态基础描述，不拼接运行时数值。
- `IconPath`：图标资源路径。
- `Trigger`：何时进入判断。
- `Condition`：触发后使用的主要条件处理器。
- `Effect`：条件成立后执行的效果处理器。
- `TargetWeaponId`：为空表示适用于角色或全局。
- `RequiredPassiveId`：组合能力的前置被动。
- `RequiredCardId`：卡牌接线前置。
- `RequiredElement`：元素限制。
- `RequiredReaction`：反应限制。
- `CooldownSeconds`：成功执行后开始计算。
- `Radius`：空间查询范围；不需要空间查询时为 0。
- `Value`：主要倍率或数值。
- `SecondaryValue`：次要倍率或数值。
- `Count`：数量或层数。
- `Priority`：同一事件的执行顺序，数值大者优先。
- `MaximumTriggersPerSecond`：单实例速率限制。
- `AllowSelfTriggeredEvents`：是否允许处理由自身产生的事件，默认关闭。
- `EnabledByDefault`：用于内容调试和灰度开关。

### 8.3 条件枚举

```csharp
public enum PassiveConditionKind : byte
{
    Always,
    CooldownReady,
    TargetAlive,
    TargetIsMarked,
    TargetIsElite,
    TargetHasElement,
    TargetHasReactionPotential,
    MinimumNearbyEnemies,
    MinimumNearbyReactionTargets,
    DeploymentBelowLimit,
    DeploymentAtLimit,
    ChargeFull,
    PlayerHealthBelowFraction,
    WeaponMatches,
    RandomProc
}
```

`RandomProc` 必须使用专属确定性随机流 `PassiveProc`，不得使用表现随机流。

### 8.4 效果枚举

```csharp
public enum PassiveEffectKind : byte
{
    None,
    ModifyCatalysisCharge,
    ExecuteCatalysis,
    ApplyElement,
    ApplyMark,
    SpreadMark,
    SpawnProjectile,
    RecallProjectile,
    DuplicateProjectile,
    CreateDeployment,
    TriggerOldestDeployment,
    TriggerNearestDeployment,
    CreateGroundField,
    ModifyNextWeaponCast,
    ReduceCooldown,
    GrantTemporaryStat,
    EmitReactionAction
}
```

复杂效果使用 `PassiveEffectKind` 定位处理器，具体算法由代码实现。不得将复杂逻辑压缩到 `Value` 和 `Count` 的隐含约定中；如果一个效果需要三个以上专属参数，应建立专属定义或专属 Handler。

## 9. PassiveRuntime

### 9.1 运行时字段

```csharp
public sealed class PassiveRuntime
{
    public PassiveDefinition Definition { get; }
    public bool Enabled { get; set; }
    public float CooldownRemaining { get; set; }
    public int TriggersThisSecond { get; set; }
    public double RateWindowStartedAt { get; set; }
    public ulong LastProcessedSequence { get; set; }
    public ulong LastTriggeredSequence { get; set; }
    public int TotalTriggerCount { get; set; }
    public double TotalValueProduced { get; set; }
}
```

### 9.2 更新规则

- 冷却只在 `RunState.Playing` 时减少。
- 暂停和升级界面不推进冷却。
- 条件失败不消耗冷却。
- 效果实际执行成功后才消耗冷却。
- 如果效果没有合法目标，视为执行失败。
- 同一 `Sequence` 每个被动实例最多处理一次。

## 10. PassiveSystem

### 10.1 节点位置

建议加入：

```text
RunRoot
└─ SimulationRoot
   ├─ PassiveEventBus
   ├─ PassiveSystem
   ├─ PassiveEffectExecutor
   └─ AutoCatalysisSystem
```

### 10.2 生命周期

`_Ready()`：

1. 获取 `SaveService.ActiveCharacter`。
2. 加载角色核心被动。
3. 监听 `SpellSystem.LoadoutChanged`。
4. 从已安装卡牌建立卡牌被动实例。
5. 建立按 `PassiveTriggerKind` 分组的索引。

每个物理帧：

1. 更新所有被动冷却。
2. 产生到期的 `Interval` 事件。
3. 从事件总线取出事件快照。
4. 按事件序号处理。
5. 同一事件中的被动按 `Priority` 降序、`Id` 升序处理。
6. 将成功效果写入效果执行队列。
7. 在被动阶段末执行效果队列。
8. 效果产生的新事件进入下一轮队列，不在当前遍历中插入。

### 10.3 重新构建索引

发生以下情况时重新构建：

- 获得新武器。
- 安装或升级卡牌。
- 角色被动因剧情或模式变化。
- 测试工具强制刷新。

不得每帧扫描整个卡牌目录。

## 11. 自动催化系统

### 11.1 设计目标

自动催化必须满足：

- 不在没有收益时浪费充能。
- 能被玩家通过走位间接控制。
- 触发结果稳定、可预测、可解释。
- 不需要鼠标位置、右摇杆或技能按钮。
- 在大量敌人场景中保持固定上限的计算量。

### 11.2 状态枚举

```csharp
public enum CatalysisState : byte
{
    Charging,
    Evaluating,
    ReadyWaiting,
    Executing,
    CooldownSuppressed
}
```

### 11.3 配置字段

```csharp
[GlobalClass]
public partial class AutoCatalysisDefinition : ContentDefinition
{
    [Export] public float ChargeDuration { get; set; } = 5.0f;
    [Export] public float SearchRadiusFromPlayer { get; set; } = 10.0f;
    [Export] public float EffectRadius { get; set; } = 4.5f;
    [Export] public float EvaluationInterval { get; set; } = 0.15f;
    [Export] public float MinimumScore { get; set; } = 3.0f;
    [Export] public int MinimumReactiveTargets { get; set; } = 2;
    [Export] public int MaximumCandidateCenters { get; set; } = 24;
    [Export] public int MaximumTargetsEvaluatedPerCandidate { get; set; } = 64;
    [Export] public float EliteScoreBonus { get; set; } = 1.5f;
    [Export] public float BossScoreBonus { get; set; } = 3.0f;
    [Export] public float ExpectedDamageWeight { get; set; } = 0.02f;
    [Export] public float ReactionCountWeight { get; set; } = 1.0f;
    [Export] public float DistancePenaltyWeight { get; set; } = 0.04f;
    [Export] public float HoldChargeMaximumSeconds { get; set; } = 8.0f;
    [Export] public float ForcedTriggerScoreMultiplier { get; set; } = 0.65f;
}
```

### 11.4 运行时字段

```csharp
public sealed class AutoCatalysisRuntime
{
    public CatalysisState State { get; set; }
    public float Charge { get; set; }
    public float EvaluationRemaining { get; set; }
    public float ReadyWaitingSeconds { get; set; }
    public Vector2 BestPosition { get; set; }
    public float BestScore { get; set; }
    public int BestReactiveTargetCount { get; set; }
    public int TotalExecutions { get; set; }
    public int TotalReactionsTriggered { get; set; }
}
```

`Charge` 使用 0–1 范围。HUD 不应自行根据时间重新计算。

### 11.5 充能逻辑

```text
如果 RunState != Playing：停止推进
如果 Charge < 1：
    Charge += delta / ChargeDuration
    State = Charging
如果 Charge >= 1：
    State = Evaluating
    按 EvaluationInterval 周期执行候选评分
```

充满后不会自动归零。只有实际执行成功才重置为 0。

### 11.6 候选中心

候选中心来自玩家周围 `SearchRadiusFromPlayer` 内的敌人位置。

筛选顺序：

1. 移除无效或死亡实体。
2. 移除当前完全没有可催化组合的实体。
3. 精英与 Boss 优先保留。
4. 普通敌人按与玩家距离升序、实体 ID 升序保留。
5. 总数截断到 `MaximumCandidateCenters`。

不额外采样空地中心。这样候选结果更容易解释，也能控制成本。

### 11.7 评分公式

每个候选中心查询 `EffectRadius` 内的目标，计算：

```text
score =
    可触发反应数量 × ReactionCountWeight
  + 预计反应伤害 × ExpectedDamageWeight
  + 区域内精英数量 × EliteScoreBonus
  + 区域内 Boss 数量 × BossScoreBonus
  - 候选中心到玩家距离 × DistancePenaltyWeight
```

预计反应伤害必须通过元素系统的只读预览接口获得，不得为了评分修改真实元素层数。

建议新增：

```csharp
public readonly record struct ReactionPreview(
    bool CanReact,
    ReactionKind Reaction,
    int ConsumedStacks,
    float ExpectedDamage);

public bool TryPreviewCatalysis(
    in ElementRuntimeState state,
    out ReactionPreview preview);
```

### 11.8 决胜规则

多个候选分数相同时依次比较：

1. 可触发目标数量更多者优先。
2. 包含 Boss 者优先。
3. 包含精英更多者优先。
4. 距离玩家更近者优先。
5. 候选中心实体 ID 更小者优先。

不得使用随机数打破平局。

### 11.9 触发条件

正常触发必须同时满足：

```text
Charge >= 1
BestScore >= MinimumScore
BestReactiveTargetCount >= MinimumReactiveTargets
```

充满后等待超过 `HoldChargeMaximumSeconds` 时，可以使用：

```text
forcedMinimumScore = MinimumScore × ForcedTriggerScoreMultiplier
```

强制触发仍然要求至少存在 1 个可反应目标。没有反应目标时继续保持充能，绝不空放。

### 11.10 执行逻辑

1. 锁定本次评估得到的位置。
2. 再次查询区域内目标，排除已死亡目标。
3. 按实体 ID 升序提交 `ForceReactionCheck`。
4. 统计实际触发反应数与预计值差异。
5. 至少一个目标实际完成催化后视为成功。
6. 成功后 `Charge = 0`，状态回到 `Charging`。
7. 发布 `CatalysisExecuted` 事件。
8. 更新远征统计。

如果执行前目标全部死亡，不消耗充能，下次评估重新选择。

### 11.11 玩家间接控制方式

- 靠近更多带元素敌人，提高候选区域分数。
- 引导精英进入普通敌人群，提高精英加权收益。
- 通过升级改变最低目标数、范围、充能速度与评分倾向。
- 通过闪避穿过敌群，触发角色被动并改变区域状态。

## 12. 三名角色逻辑

以下数值为首轮平衡基线，最终以实测为准。

### 12.1 角色定义字段调整

现有 `CharacterDefinition` 建议调整为：

```csharp
public sealed record CharacterDefinition(
    string Id,
    string DisplayName,
    string Role,
    string Description,
    string StartingWeaponId,
    string StartingWeaponName,
    SpellCastKind PrototypeStartingSpell,
    IReadOnlyList<string> CorePassiveIds,
    IReadOnlyList<string> CompatibleWeaponIds,
    string UnlockHint,
    string ArchiveRecordId);
```

删除仅用于显示但不受运行时约束的 `ActiveAbility` 字段。角色页从真实 `CorePassiveIds` 读取名称和描述。

### 12.2 元素行者

#### 核心定位

稳定、高频、擅长组合不同元素，适合理解自动反应系统。

#### 被动 A：优化催化协议

```text
ID：passive.elementalist.catalysis_optimizer
触发：系统初始化
效果：
- AutoCatalysis.ChargeDuration × 0.85
- AutoCatalysis.MinimumScore × 0.90
- 催化评分额外偏好区域内不同反应种类
```

不同反应种类奖励：每种额外反应类型增加 0.5 分，最多增加 1.5 分。

#### 被动 B：载荷适配

```text
ID：passive.elementalist.payload_adapter
触发：安装本局第一张载荷卡
条件：尚未使用本局折扣
效果：该卡最终功耗减少 1，最低为 0
```

运行时必须记录折扣具体作用的卡牌 ID，重建构筑时保持一致。

#### 推荐构筑行为

- 让多个武器携带不同基元。
- 通过走位将不同状态敌人聚集。
- 提高自动催化频率和反应多样性。

### 12.3 回响猎手

#### 核心定位

依靠标记、弹射和返程攻击重复命中关键目标。

#### 被动 A：回响追猎

```text
ID：passive.echo_hunter.mark_priority
触发：弹射目标选择
效果：目标评分中，标记目标获得 +1000 固定优先级
```

目标选择顺序：

1. 未命中过且带标记的合法目标。
2. 未命中过的合法目标。
3. 如果允许重复命中，再选择标记层数最高目标。
4. 距离更近者优先。
5. 实体 ID 更小者优先。

#### 被动 B：自动回收

```text
ID：passive.echo_hunter.auto_recall
触发：圆盘剩余弹射次数为 0，或存活时间达到上限的 70%
效果：圆盘自动返回玩家，返程获得 1 次额外穿透
内部冷却：每个投射物只允许执行 1 次
```

返程路径以当前玩家位置持续更新，但转向速度有上限，避免瞬时折线。

#### 被动 C：标记扩散

```text
ID：passive.echo_hunter.mark_spread
触发：带标记敌人死亡
条件：附近 5m 内存在未标记敌人
效果：向最近的最多 2 个敌人传播 1 层标记
内部冷却：0.1 秒
```

传播优先级：距离、生命值比例、实体 ID。

### 12.4 裂隙工匠

#### 核心定位

通过地雷、领域和连锁引爆控制空间。

#### 被动 A：容量保护

```text
ID：passive.rift_engineer.capacity_safety
触发：部署物数量达到上限且即将创建新部署物
效果：先触发最早创建的合法部署物，再创建新部署物
```

如果最早部署物正在触发，则选择下一个。所有部署物均不可触发时，取消本次创建，不覆盖现有部署物。

#### 被动 B：超载监测

```text
ID：passive.rift_engineer.elite_overload
触发：精英或 Boss 进入部署物作用范围
条件：至少 2 个部署物可以覆盖该目标
效果：触发距离目标最近的部署物，并强化下一台创建的部署物
冷却：6 秒
```

强化内容基线：伤害 × 1.25，范围 × 1.15。强化只影响下一台部署物，不叠加。

#### 被动 C：闪避布雷

```text
ID：passive.rift_engineer.dodge_mine
触发：DodgeEnded
条件：部署物数量低于上限
效果：在闪避终点创建一枚弱化地雷
冷却：2.5 秒
```

弱化地雷参数：伤害 × 0.45、范围 × 0.8、不触发“闪避布雷”类衍生事件。

## 13. 闪避系统调整

`PlayerController` 应发布包含位置的两个事件：

```csharp
public event Action<Vector2, Vector2>? DodgeStarted;
public event Action<Vector2>? DodgeEnded;
```

参数：

- `DodgeStarted(startPosition, direction)`。
- `DodgeEnded(endPosition)`。

闪避结束判定为 `_dodgeRemaining` 从正数变为 0 的那个物理帧。不得仅在按键按下时同时发布开始与结束。

HUD 继续显示闪避冷却，但闪避不进入技能栏。

## 14. 卡牌与被动接线

### 14.1 卡牌分类

| 分类 | 职责 | 示例 |
| --- | --- | --- |
| 行为 | 改变投射物或部署物如何运动 | 穿透、弹射、分裂、回旋 |
| 载荷 | 决定施加的元素基元 | 火、水、风、土、雷、标记 |
| 动作 | 定义可被执行的结果 | 爆炸、碎片、新星、领域 |
| 触发 | 定义何时执行动作 | 穿透时、弹射时、冻结时、闪避结束时 |
| 反应 | 改写已有状态或反应结果 | 碎冰、燃爆传播、死亡新星 |

### 14.2 接线规则

- 触发卡必须连接到至少一个动作卡才可进入候选池。
- 动作卡可以先安装并等待触发卡。
- 每个武器默认只有一个主动作接线槽。
- 同一触发事件只执行一次主动作，额外动作通过卡牌明确增加。
- 触发卡不会自动触发所有已安装动作。
- UI 必须显示当前接线结果。

### 14.3 建议运行时结构

```csharp
public sealed record CardConnection(
    string TriggerCardId,
    string ActionCardId,
    int Priority,
    bool Enabled);

public sealed class WeaponBuildRuntime
{
    public List<CardConnection> Connections { get; } = new();
}
```

### 14.4 功耗

```text
最终功耗 = 武器专属卡功耗
         + 标准卡功耗
         - 角色折扣
         - 本局临时折扣
```

- 最终功耗最低为 0。
- 折扣必须绑定具体卡牌 ID。
- 达到容量上限后，不再生成无法安装的选项。
- UI 必须显示安装前后功耗与剩余容量。

## 15. 升级选择组件逻辑

### 15.1 UpgradeChoice 数据调整

```csharp
public sealed record UpgradeChoice(
    string Id,
    string DisplayName,
    string Description,
    float Weight,
    UpgradeChoiceKind Kind,
    string TargetWeaponId,
    UpgradeRarity Rarity,
    int PowerBefore,
    int PowerAfter,
    IReadOnlyList<StatDelta> StatChanges,
    IReadOnlyList<string> SynergyTagIds,
    IReadOnlyList<CardConnectionPreview> ConnectionChanges,
    string FusionBeforeId,
    string FusionAfterId,
    string PreviewText,
    string WarningText,
    UpgradeDefinition? GeneralDefinition = null,
    SpellCastKind Spell = SpellCastKind.ArcaneMissile,
    SpellBranch Branch = SpellBranch.None,
    WeaponCardDefinition? WeaponCard = null,
    StandardCardDefinition? StandardCard = null);
```

辅助类型：

```csharp
public readonly record struct StatDelta(
    string StatId,
    string DisplayName,
    float Before,
    float After,
    string Format);

public readonly record struct CardConnectionPreview(
    string TriggerName,
    string ActionName,
    bool IsNew);
```

### 15.2 升级卡组件

每张升级卡必须包含：

1. 稀有度条。
2. 图标。
3. 名称。
4. 目标武器。
5. 卡牌类别。
6. 功耗变化。
7. 一句行为变化描述。
8. 最多三个主要数值变化。
9. 元素、反应或接线标签。
10. 安装后的简短预览。

如果没有数值变化，必须显示具体行为变化，不能使用“显著增强”等模糊描述。

### 15.3 候选生成逻辑

1. 为当前角色建立兼容内容池。
2. 移除未解锁内容。
3. 移除达到最高等级的卡牌。
4. 移除功耗不足且没有合法折扣的卡牌。
5. 移除缺少必要动作接线的触发卡。
6. 根据当前构筑计算协同权重。
7. 使用 `RunRandomStreams.Upgrade` 加权抽取。
8. 同一批三个选项不得拥有相同稳定 ID。
9. 生成完整安装预览。
10. 选中后重新验证合法性，再执行安装。

### 15.4 刷新

- 默认每局 1 次。
- 刷新不推进战斗时间。
- 刷新使用同一升级随机流的后续状态。
- 刷新后的三个选项不得与刷新前完全相同；尝试 8 次后允许部分重复。
- 刷新次数进入远征统计。

## 16. HUD 组件逻辑

### 16.1 HUD 总原则

- 不显示可点击或带按键提示的技能按钮。
- 自动系统必须显示状态，而非伪装成主动技能。
- 中央战斗区域保持开放。
- 所有元素同时使用颜色和符号。

### 16.2 催化协议组件

组件字段：

```text
CatalysisPanel
├─ StateIcon
├─ StateLabel
├─ ChargeProgress
├─ CandidateCountLabel
├─ RequirementLabel
└─ LastResultLabel
```

状态显示：

| 状态 | 主文本 | 副文本 |
| --- | --- | --- |
| Charging | 催化协议充能中 | `72%` |
| Evaluating | 正在评估反应区域 | `候选 3` |
| ReadyWaiting | 催化协议待触发 | `需要至少 2 个有效目标` |
| Executing | 自动催化 | `预计触发 5 次反应` |
| CooldownSuppressed | 催化协议暂停 | 显示暂停原因 |

触发后显示 1.5 秒结果：

```text
自动催化完成 · 触发 4 次反应 · 造成 368 反应伤害
```

不得在屏幕上显示 `Q`、右键或扳机图标。

### 16.3 武器槽组件

每个槽显示：

- 武器图标。
- 等级。
- 当前自动攻击冷却进度。
- 载荷元素符号。
- 当前功耗 / 容量。
- 已形成接线的小型图标。

不显示按键绑定。

### 16.4 被动触发反馈

高价值被动触发使用短文本或图标反馈：

- 自动回收。
- 标记扩散。
- 超载部署。
- 催化优化。

普通高频效果不得逐次显示文本，避免信息淹没。相同被动反馈 0.5 秒内合并计数。

### 16.5 输入提示

键鼠：

```text
WASD 移动 · Space 闪避 · Esc 暂停
```

手柄：

```text
左摇杆移动 · A 闪避 · 菜单键暂停
```

## 17. 远征统计

### 17.1 RunStatistics 新增字段

```csharp
public int DodgeCount { get; private set; }
public int CatalysisChargeCompletedCount { get; private set; }
public int CatalysisExecutionCount { get; private set; }
public int CatalysisReactionCount { get; private set; }
public double CatalysisReactionDamage { get; private set; }
public int UpgradeRerollCount { get; private set; }
public IReadOnlyDictionary<string, int> PassiveTriggerCounts { get; }
public IReadOnlyDictionary<string, double> PassiveProducedValue { get; }
public IReadOnlyList<RunObjectiveResult> ObjectiveResults { get; }
```

### 17.2 被动贡献统计

- 伤害类被动记录直接伤害。
- 扩散和复制类被动记录由该效果创建实体造成的伤害。
- 改变目标选择的被动只记录触发次数，不虚构伤害贡献。
- 催化优化记录因阈值改变而成功执行的次数，不计算假设伤害差。

## 18. 构筑快照

### 18.1 数据结构

```csharp
public sealed class RunBuildSnapshot
{
    public string CharacterId { get; set; } = string.Empty;
    public string StartingWeaponId { get; set; } = string.Empty;
    public List<string> CharacterPassiveIds { get; set; } = new();
    public List<WeaponBuildSnapshot> Weapons { get; set; } = new();
    public List<GeneralUpgradeSnapshot> GeneralUpgrades { get; set; } = new();
}

public sealed class WeaponBuildSnapshot
{
    public string WeaponId { get; set; } = string.Empty;
    public int Level { get; set; }
    public string BranchId { get; set; } = string.Empty;
    public int PowerUsed { get; set; }
    public int PowerCapacity { get; set; }
    public string FusionIdentityId { get; set; } = string.Empty;
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

public sealed class GeneralUpgradeSnapshot
{
    public string UpgradeId { get; set; } = string.Empty;
    public int Level { get; set; }
}
```

`RunSummary.FinalBuild` 的字符串列表应在完成迁移后废弃。UI 从结构化快照生成显示文本。

## 19. 存档字段

### 19.1 SchemaVersion

存档版本从 3 提升到 4。

### 19.2 ProfileData V4

```csharp
public sealed class ProfileData
{
    public int SchemaVersion { get; set; } = 4;
    public double BestSurvivalTime { get; set; }
    public int BestKillCount { get; set; }
    public bool BossDefeated { get; set; }
    public int CompletedRuns { get; set; }
    public int MemoryShards { get; set; }
    public string SelectedCharacterId { get; set; } = "character.elementalist";
    public List<string> UnlockedCharacterIds { get; set; } = new();
    public List<string> UnlockedWeaponIds { get; set; } = new();
    public List<string> UnlockedCardIds { get; set; } = new();
    public List<string> UnlockedPassiveIds { get; set; } = new();
    public Dictionary<string, CharacterProgressData> CharacterProgress { get; set; } = new();
    public HashSet<string> SeenTutorialIds { get; set; } = new();
    public HashSet<string> DiscoveredReactionIds { get; set; } = new();
    public HashSet<string> DiscoveredFusionIds { get; set; } = new();
    public HashSet<string> UnlockedRecordIds { get; set; } = new();
    public HashSet<string> ViewedRecordIds { get; set; } = new();
    public RunSummary? LastRun { get; set; }
}
```

如果当前 JSON 序列化方案对 `HashSet<string>` 支持不稳定，持久化类型使用 `List<string>`，加载后在运行时建立 HashSet 索引。

### 19.3 CharacterProgressData

```csharp
public sealed class CharacterProgressData
{
    public int CompletedRuns { get; set; }
    public int Victories { get; set; }
    public int BossKills { get; set; }
    public int MasteryExperience { get; set; }
    public int HighestDifficulty { get; set; }
    public List<string> ClaimedRewardIds { get; set; } = new();
}
```

### 19.4 RunSummary V4

```csharp
public sealed class RunSummary
{
    public int SchemaVersion { get; set; } = 4;
    public string CharacterId { get; set; } = string.Empty;
    public string StartingWeaponId { get; set; } = string.Empty;
    public ulong Seed { get; set; }
    public int Difficulty { get; set; }
    public double SurvivalTime { get; set; }
    public int KillCount { get; set; }
    public bool Victory { get; set; }
    public bool BossDefeated { get; set; }
    public double DamageDealt { get; set; }
    public double DamageTaken { get; set; }
    public int ReactionCount { get; set; }
    public double ReactionDamage { get; set; }
    public int CatalysisExecutionCount { get; set; }
    public int CatalysisReactionCount { get; set; }
    public double CatalysisReactionDamage { get; set; }
    public int DodgeCount { get; set; }
    public int RerollCount { get; set; }
    public int MemoryShardsEarned { get; set; }
    public string Result { get; set; } = string.Empty;
    public string DeathCause { get; set; } = string.Empty;
    public Dictionary<string, double> DamageBySpell { get; set; } = new();
    public Dictionary<string, int> ReactionCounts { get; set; } = new();
    public Dictionary<string, int> PassiveTriggerCounts { get; set; } = new();
    public List<RunObjectiveResult> ObjectiveResults { get; set; } = new();
    public RunBuildSnapshot Build { get; set; } = new();
    public List<string> NewlyDiscoveredIds { get; set; } = new();
    public List<string> NewlyUnlockedIds { get; set; } = new();
    public DateTimeOffset CompletedAtUtc { get; set; }
}
```

### 19.5 V3 → V4 迁移

1. 读取旧存档但不立即覆盖。
2. 将 `CharacterMastery` 的局数复制到 `CharacterProgress.CompletedRuns`。
3. 保留所有已解锁角色和武器。
4. 根据默认初始内容填充 `UnlockedPassiveIds`。
5. 旧 `TutorialSeen = true` 时加入基础移动与升级教程 ID。
6. 旧 `LastRun.FinalBuild` 无法可靠解析时保留为迁移备注，不推测卡牌 ID。
7. 完成校验后原子保存 V4。
8. 迁移失败时继续保留 V3 文件并创建错误日志，不写入空存档。

## 20. 确定性与随机流

`RunRandomStreams` 增加：

```csharp
public RandomNumberGenerator PassiveProc { get; }
public RandomNumberGenerator TargetTieBreak { get; }
```

实际建议避免 `TargetTieBreak`，目标平局优先使用实体 ID。只有明确要求随机的卡牌使用 `PassiveProc`。

确定性规则：

- 空间查询结果在处理前按实体 ID 排序。
- 被动按优先级和稳定 ID 排序。
- 同分候选不使用随机数。
- 表现粒子和声音变体继续使用 `Presentation` 流。
- UI 动画不得消耗玩法随机流。
- 暂停时不推进玩法计时或随机状态。

## 21. 性能要求

### 21.1 目标

- 500 个普通敌人存在时，被动系统不进行每个被动 × 每个敌人的全量扫描。
- 自动催化默认每 0.15 秒评估一次，而不是每帧。
- 空间查询复用现有 `SpatialGrid`。
- 事件和效果列表复用缓冲区，避免每帧分配大型集合。
- 单个被动效果最多处理 64 个目标。

### 21.2 禁止模式

- 每个敌人挂载独立 Godot 计时器作为被动触发器。
- 每个被动每帧调用全场实体查询。
- 在事件回调内立即触发下一层事件并递归调用。
- 使用 LINQ 处理高频战斗热路径。
- 为每次命中创建临时字典或列表。

## 22. 调试功能

调试 HUD 增加：

```text
Passive events/frame
Passive effects/frame
Dropped passive events
Maximum chain depth this frame
Catalysis state
Catalysis charge
Best candidate score
Best candidate target count
Active passive count
```

调试命令或测试入口：

- 强制催化充满。
- 显示所有催化候选区域和分数。
- 禁用指定被动 ID。
- 强制发布某种被动事件。
- 导出当前构筑快照。
- 输出最近 50 条被动事件链。

开发可视化不得进入正式发布版本或必须由调试开关控制。

## 23. 自动化测试

### 23.1 单元测试

新增建议：

```text
tests/unit/PassiveEventBusTests.cs
tests/unit/PassiveConditionTests.cs
tests/unit/PassiveBudgetTests.cs
tests/unit/AutoCatalysisScoringTests.cs
tests/unit/CharacterPassiveTests.cs
tests/unit/BuildSnapshotTests.cs
tests/unit/ProfileV4MigrationTests.cs
```

必须覆盖：

- 事件按序号顺序分发。
- 同一被动不重复处理同一事件。
- 条件失败不消耗冷却。
- 超过链深后停止产生衍生事件。
- 催化未达到分数时保持满充能。
- 没有反应目标时绝不空放。
- 同分候选按固定规则选出相同目标。
- 元素行者载荷折扣只使用一次。
- 圆盘只自动召回一次。
- 工匠达到部署上限时触发最早部署物。
- 构筑快照包含全部卡牌与接线。
- V3 存档迁移后不丢失角色、武器和货币。

### 23.2 集成测试

场景测试至少覆盖：

1. 生成多个带反应条件的敌人。
2. 等待催化充满。
3. 验证系统选择最高分区域。
4. 验证催化执行后清空充能。
5. 验证 HUD 从“充能中”切换到“自动催化完成”。
6. 验证统计记录实际反应数。
7. 完成对局并检查构筑快照。

### 23.3 性能测试

在 500 敌人压力场景中验证：

- 被动事件队列不持续溢出。
- 自动催化评估时间稳定。
- 不出现随运行时间持续增长的集合。
- 同 Seed 运行两次得到相同催化目标序列。

## 24. 验收标准

### 24.1 操作

- 键鼠和手柄均不需要技能按键。
- HUD 不显示主动技能按钮或相关按键。
- 玩家只使用移动和闪避即可完成完整对局。

### 24.2 自动催化

- 催化充满后不会在空地释放。
- 玩家可以通过聚怪明显改变催化触发时机与结果。
- HUD 能解释当前正在充能、评估还是等待目标。
- 同一测试场景中候选选择可复现。

### 24.3 角色

- 三个角色至少各有两个实际生效的核心被动。
- 更换角色会改变目标选择、自动攻击或部署逻辑。
- 角色详情页显示的能力全部来自真实运行时定义。

### 24.4 构筑

- 升级界面显示功耗、数值变化和接线结果。
- 触发卡不能在没有合法动作时作为无效选项出现。
- 结算页能够还原完整构筑。

### 24.5 稳定性

- 被动触发链存在深度和数量上限。
- 高实体数量下不存在无限递归或明显持续分配。
- V3 存档可以安全迁移到 V4。

## 25. 实施顺序

### 阶段 A：移除主动输入

1. 删除催化输入映射和提示。
2. 保留现有催化执行函数，移除输入读取。
3. HUD 将催化按钮改为状态面板。
4. 更新烟雾测试中的输入提示断言。

### 阶段 B：自动催化

1. 增加反应只读预览接口。
2. 实现充能状态机。
3. 实现候选查询、评分和确定性决胜。
4. 接入 HUD 和统计。
5. 增加自动催化单元与集成测试。

### 阶段 C：统一被动框架

1. 增加事件总线和预算。
2. 接入闪避、命中、反应、击杀事件。
3. 实现被动运行时和效果执行队列。
4. 将现有标准触发卡接入统一事件语义。

### 阶段 D：角色被动

1. 元素行者催化优化与载荷折扣。
2. 回响猎手标记优先、自动回收与扩散。
3. 裂隙工匠容量保护、超载与闪避布雷。
4. 删除角色定义中的主动能力展示字段。

### 阶段 E：构筑与存档

1. 扩充升级选项预览字段。
2. 实现卡牌接线快照。
3. 升级 `RunSummary` 与 `ProfileData`。
4. 完成 V3 → V4 迁移。
5. 更新远征报告。

## 26. 后续扩展边界

在基础系统稳定后，可以增加：

- 基于生命比例的防御被动。
- 改变催化目标评分的卡牌。
- 让某些武器共享触发事件的跨武器接线。
- 裂隙词缀对被动条件的修改。
- 异常卡牌带来的高收益自动循环。
- 构筑预设和历史构筑复现。

暂不建议：

- 可以任意组合的可视化脚本语言。
- 无上限的跨武器递归触发。
- 在同一局中切换角色被动。
- 需要玩家临时按键关闭或开启被动的姿态系统。

## 27. 开发检查清单

实现任何新被动前必须回答：

1. 它由哪个 `PassiveTriggerKind` 触发？
2. 它的条件为什么对玩家可理解？
3. 没有合法目标时是否消耗冷却？
4. 它能否由自己产生的事件再次触发？
5. 最大链深和单次目标数是多少？
6. 是否需要确定性随机流？
7. HUD 是否需要反馈，反馈是否会过于频繁？
8. 如何计入统计和构筑快照？
9. 同 Seed 下是否可复现？
10. 对 500 敌人场景的最坏计算成本是多少？

无法回答以上问题的被动不得进入正式内容目录。
