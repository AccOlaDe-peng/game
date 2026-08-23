# Project Catalyst Godot 技术设计文档

> 文档编号：CAT-TDD-GODOT-001  
> 版本：0.1.0  
> 状态：纵向切片开发基线  
> 更新日期：2026-08-19  
> 对应产品文档：[PRODUCT_SPEC.md](./PRODUCT_SPEC.md)  
> 引擎基线：Godot 4.7.1 Stable .NET

## 1. 文档目的

本文档定义 Project Catalyst 使用 Godot 实现纵向切片时的技术栈、C#代码边界、场景结构、数据模型、大规模单位模拟、渲染策略、测试要求和开发里程碑。

技术优先级：

1. 快速验证元素反应是否构成可重复游玩的核心体验。
2. 完整支持目标15分钟单局的开始、升级、事件、首领、胜负和结算。
3. 在500个普通敌人、800–1000个投射物的压力目标下保持稳定帧率。
4. 让法术、敌人、升级、波次和反应通过Godot Resource配置。
5. 核心规则可自动测试，并能通过固定随机种子复现。
6. 不在没有性能数据时提前引入C++ GDExtension或多线程复杂度。

## 2. 引擎、语言与平台

### 2.1 引擎版本

- 使用 Godot 4.7.1 Stable .NET 版本。
- 不使用4.7.2 RC、4.8开发版或自行编译的引擎主干。
- 首次提交记录完整引擎版本；团队统一版本。
- 进入M1后不自动升级引擎，升级必须建立独立分支并完成测试与导出回归。

当前正式稳定版本可在[Godot官方版本列表](https://godotengine.org/download/archive/)确认。

### 2.2 编程语言

- 主要语言：C#。
- 运行时：Godot .NET编辑器要求的稳定.NET SDK版本。
- 核心玩法不混用GDScript，避免双语言调用、调试和风格成本。
- Shader使用Godot Shader Language。
- 只有经过Profiler确认的纯计算瓶颈才评估C++ GDExtension。

C#适用于Windows桌面导出；Godot 4的C#项目当前不能导出Web，本项目纵向切片不以Web为目标。官方参考：[Godot C#/.NET](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html)。

### 2.3 渲染和目标平台

- 渲染器：Forward+。
- 目标平台：Windows x86_64。
- 开发运行：Editor Debug。
- 日常独立测试：Windows Debug Export。
- 性能验收：Windows Release Export。
- 初始图形API使用Vulkan；同时在目标机器验证D3D12驱动表现。

### 2.4 开发环境

- 安装Godot 4.7.1 .NET编辑器和对应导出模板。
- 安装Godot版本要求的64位.NET SDK，并记录`dotnet --info`。
- 推荐IDE：JetBrains Rider、Visual Studio 2022或VS Code。
- 项目必须可以在没有个人EditorPlugin和全局脚本的干净环境中打开、编译和导出。
- 第三方插件进入项目`addons/`，记录来源、许可证、版本和用途。

## 3. 已锁定技术决策

| 类别 | 决策 |
|---|---|
| 引擎 | Godot 4.7.1 Stable .NET |
| 核心语言 | C# |
| 渲染器 | Forward+ |
| 玩家 | `CharacterBody3D`独立场景 |
| Boss/精英 | 独立场景节点，注册到统一战斗系统 |
| 普通怪物 | C#中央数组模拟 + MultiMesh批量表现 |
| 投射物 | C#中央数组模拟 + MultiMesh/GPUParticles3D表现 |
| 经验物 | 中央数组模拟 + MultiMesh表现 |
| 碰撞查询 | XZ平面固定空间网格，不为普通攻击创建大量`Area3D` |
| 静态配置 | 自定义`Resource`与`.tres` |
| 运行状态 | C#结构体/类，不修改配置Resource |
| 对局服务 | Run场景内组合，不滥用Autoload |
| 输入 | InputMap动作，键鼠和手柄共用语义动作 |
| 存档 | `user://`版本化JSON，临时文件后替换 |
| 随机性 | 每局根种子，按系统拆分`RandomNumberGenerator` |
| 测试 | NUnit纯逻辑测试 + Godot Headless集成/压力测试 |

## 4. 明确不采用的方案

纵向切片不采用：

- 为每个普通怪创建`CharacterBody3D`和独立`_PhysicsProcess()`。
- 为每颗投射物创建`Area3D`、碰撞Shape和独立逐帧脚本。
- 使用Godot物理引擎处理所有法术命中和怪物分离。
- 把所有服务放入Autoload形成全局可变状态。
- 在运行时修改`.tres`配置资源。
- 核心系统同时维护GDScript和C#版本。
- 首版直接编写C++ GDExtension。
- 在Profiler确认前引入多线程模拟。
- 依赖不固定版本的第三方玩法框架。

## 5. 版本管理

### 5.1 Git提交范围

必须提交：

- `project.godot`
- `*.csproj`和必要的Solution配置
- `scenes/`、`scripts/`、`resources/`、`assets/`
- `addons/`中的项目级插件
- `tests/`
- `export_presets.cfg`中的非敏感导出配置
- Godot生成并用于资源引用稳定性的`.uid`文件
- `docs/`

必须忽略：

- `.godot/`
- `.mono/`（若版本产生）
- `bin/`、`obj/`
- IDE个人缓存和用户设置
- 导出产物、日志、Profiler捕获和本地临时文件
- `user://`运行时存档

### 5.2 Git LFS

以下二进制源资产使用Git LFS：

- Blender、FBX、GLB源文件。
- PSD、Krita、高清纹理源文件。
- WAV、FLAC和大型音频工程文件。
- 视频和大型参考资料。

Godot的`.tscn`和`.tres`通常为文本格式，不加入LFS；导入后的`.godot/imported`缓存不提交。

### 5.3 变更纪律

- 每个可交付构建对应一个可追踪的Git提交。
- 高冲突主场景拆为子场景，不让多人长期同时编辑一个大型`.tscn`。
- 移动资源后同步提交`.uid`及所有引用变化。
- 插件升级必须单独提交并记录回归结果。
- 未经确认不提交编辑器自动重写的大量无关资源差异。

## 6. 项目目录

```text
res://
├── project.godot
├── Catalyst.csproj
├── scenes/
│   ├── app/
│   ├── menu/
│   ├── run/
│   ├── player/
│   ├── enemies/
│   ├── events/
│   ├── ui/
│   └── tests/
├── scripts/
│   ├── app/
│   ├── core/
│   ├── run/
│   ├── combat/
│   ├── elements/
│   ├── spells/
│   ├── enemies/
│   ├── projectiles/
│   ├── upgrades/
│   ├── waves/
│   ├── save/
│   ├── presentation/
│   └── ui/
├── resources/
│   ├── characters/
│   ├── spells/
│   ├── reactions/
│   ├── enemies/
│   ├── upgrades/
│   ├── waves/
│   ├── events/
│   └── tuning/
├── assets/
│   ├── models/
│   ├── textures/
│   ├── materials/
│   ├── shaders/
│   ├── vfx/
│   ├── audio/
│   └── fonts/
├── addons/
├── tests/
│   ├── unit/
│   ├── integration/
│   └── performance/
└── docs/
```

资源路径使用小写英文和`snake_case`，C#类型使用`PascalCase`。

## 7. 场景结构

### 7.1 应用入口

```text
app_root.tscn
├── SceneRouter
├── TransitionLayer
└── CurrentScreen
```

`app_root.tscn`是Main Scene。它只负责主菜单、Run场景和结算流程的切换，不承载战斗逻辑。

### 7.2 主菜单

```text
main_menu.tscn
└── MainMenu (Control)
    ├── StartButton
    ├── SettingsButton
    └── QuitButton
```

### 7.3 对局场景

```text
run_arena_01.tscn
└── RunRoot (Node)
    ├── RunController
    ├── WorldRoot (Node3D)
    │   ├── Arena
    │   ├── Player
    │   ├── EnemyPresentation
    │   ├── ProjectilePresentation
    │   ├── PickupPresentation
    │   └── VfxRoot
    ├── SimulationRoot (Node)
    │   ├── EntitySystem
    │   ├── SpatialGrid
    │   ├── CombatSystem
    │   ├── ElementSystem
    │   ├── SpellSystem
    │   ├── ProjectileSystem
    │   ├── PickupSystem
    │   ├── WaveDirector
    │   └── RunStatistics
    └── UI (CanvasLayer)
        ├── RunHud
        ├── UpgradeScreen
        ├── PauseMenu
        └── ResultsScreen
```

### 7.4 性能测试场景

`performance_arena.tscn`复用正式模拟系统，但使用固定种子和测试配置，可以自动生成目标数量并输出结果。

## 8. Autoload边界

只允许以下跨场景服务成为Autoload：

- `SettingsService`
- `SaveService`
- `ContentCatalog`
- `AudioService`（仅在需要跨场景音乐连续播放时）

不得成为Autoload：

- 战斗、元素、敌人、投射物和波次系统。
- 当前玩家引用。
- 当前对局数据。
- 当前地图事件。

对局服务由`RunRoot`创建和销毁，重新开始对局时必须得到全新状态。

## 9. 对局状态机与时间

### 9.1 主状态

```text
Loading
→ Starting
→ Playing
↔ LevelUp
↔ Paused
→ BossIntro
→ Playing
→ Victory / Defeat
→ Results
→ Unloading
```

只有`RunController`可以改变主状态。UI通过只读快照和信号获知状态，不直接修改模拟系统。

### 9.2 时间源

定义：

- `RunClock`：对局模拟时间，升级和暂停时停止。
- `RealClock`：菜单、过渡和允许暂停时播放的UI动画。

模拟系统接收`RunClock.Delta`，避免到处直接读取引擎时间。

### 9.3 暂停策略

- 优先使用`SceneTree.Paused`暂停物理和普通处理。
- UI节点设置允许在暂停时处理。
- 中央模拟系统在非Playing状态下不推进。
- 升级界面和暂停界面分别管理输入焦点。
- 自动测试验证暂停期间敌人、投射物、DOT、冷却和波次时间全部停止。

## 10. 输入

### 10.1 InputMap动作

```text
move_left
move_right
move_forward
move_back
aim_left
aim_right
aim_up
aim_down
dodge
catalyze
pause
ui_accept
ui_cancel
```

### 10.2 输入规则

- `InputService`把键鼠和手柄转换为统一语义输入；移动和手柄瞄准分别通过四个方向动作组合成`Vector2`。
- 键鼠催化目标使用Camera3D射线与游戏平面的交点。
- 手柄瞄准使用右摇杆和最后一次有效方向。
- 摇杆具有可配置死区。
- 当前活动设备变化时发送信号，UI切换提示图标。
- 升级和暂停时玩家玩法输入被屏蔽，UI导航仍生效。

## 11. 玩家实现

### 11.1 Player场景

```text
player_elementalist.tscn
└── Player (CharacterBody3D)
    ├── CollisionShape3D
    ├── VisualRoot
    │   ├── Mesh/Skeleton
    │   └── PlayerVfx
    ├── SpringArm3D
    │   └── Camera3D
    ├── HealthComponent
    ├── DodgeComponent
    ├── SpellLoadout
    └── CatalyzeAbility
```

### 11.2 移动

- 玩家使用`CharacterBody3D.MoveAndSlide()`与场景障碍交互。
- 游戏模拟平面是Godot 3D坐标的XZ平面，Y轴用于高度。
- 移动方向由Camera basis投影到XZ平面得到。
- 不允许帧率直接影响移动距离。

### 11.3 闪避

- C#组件维护冷却、位移曲线和无敌窗口。
- 无敌通过明确状态标记判断，不通过临时删除碰撞节点实现。
- 位移不穿越不可穿越的场景障碍。
- 动画、声音、拖尾和屏幕反馈响应状态信号。

## 12. 数据驱动内容

### 12.1 Resource基类

```csharp
[GlobalClass]
public partial class ContentDefinition : Resource
{
    [Export] public StringName Id { get; private set; }
    [Export] public string DisplayName { get; private set; } = string.Empty;
}
```

具体资源：

- `CharacterDefinition`
- `SpellDefinition`
- `SpellUpgradeDefinition`
- `EnemyDefinition`
- `ReactionDefinition`
- `WaveDefinition`
- `RunEventDefinition`
- `TuningDefinition`

### 12.2 配置原则

- 每个内容资源具有稳定、唯一、非本地化的`StringName Id`。
- 显示名称不作为查找键。
- `.tres`只保存静态定义。
- 运行时不修改Resource字段。
- 内容引用使用导出的Resource类型，不使用散落的字符串路径。
- `ContentCatalog`启动时建立只读ID索引并执行验证。
- 同一个数值只能存在一个权威来源，禁止代码默认值和`.tres`长期重复维护。

### 12.3 运行时状态

```csharp
public sealed class SpellRuntime
{
    public required SpellDefinition Definition { get; init; }
    public int Level { get; private set; } = 1;
    public float CooldownRemaining { get; set; }
    public SpellStats Stats { get; private set; }
    public StringName SelectedBranch { get; private set; }
}
```

升级时重新聚合`SpellStats`，命中热路径只读取最终数值。

## 13. 普通敌人数据模型

### 13.1 普通怪不使用独立节点模拟

普通怪状态由`EnemySystem`集中保存。表现层中的MultiMesh实例不是玩法状态的权威来源。

```csharp
public struct EnemyState
{
    public int Generation;
    public bool Active;
    public EnemyArchetype Archetype;
    public Vector2 Position;
    public Vector2 Velocity;
    public float Health;
    public float DecisionCooldown;
    public EnemyBehaviorState Behavior;
    public ElementState Fire;
    public ElementState Frost;
    public ElementState Lightning;
}
```

`Vector2.Position`映射世界坐标`(X, Z)`。

### 13.2 EntityHandle

实体引用使用索引加代数，防止数组槽位复用后旧引用命中新实体：

```csharp
public readonly struct EntityHandle
{
    public readonly int Index;
    public readonly int Generation;
}
```

所有伤害命令执行前验证Handle仍然有效。

### 13.3 存储与容量

- 使用预分配数组和Free List管理槽位。
- M1初始容量300，M3提升到500并保留可配置余量。
- 达到硬上限时停止新生成并记录告警，不在高压帧中自动无限扩容。
- 不为每个状态创建对象或集合。
- 若Profiler显示结构数组访问成为瓶颈，再评估Structure of Arrays，不提前复杂化。

## 14. 敌人更新与行为

### 14.1 更新频率

- 移动和位置同步：统一在单个`_PhysicsProcess()`中执行。
- 近距离敌人决策：10–20Hz初始目标。
- 远距离敌人决策：更低频率。
- 敌人按桶错开决策，禁止同一帧全部重新计算。
- 死亡、受击和元素反应使用命令/事件驱动。

### 14.2 普通行为状态

```text
Spawning
→ Chasing / Positioning
→ Attacking
→ Recovering
→ Dead
```

- 群行者只需要追逐和接触攻击。
- 追猎者增加短时加速。
- 远程怪围绕期望攻击距离定位。
- 自爆怪使用明显蓄力状态。
- 召唤者以低频定时器生成请求，不自己操作实体数组。

### 14.3 移动与分离

- 直接朝玩家或期望位置转向。
- 使用空间网格查询有限邻居并计算简单分离力。
- 敌人之间不进行完整物理碰撞。
- 大体型敌人提高分离半径与权重。
- 普通竞技场保持少量规则障碍；复杂寻路不在纵向切片范围。

## 15. Boss与精英

- Boss和两种精英可以使用独立`Node3D`或`CharacterBody3D`场景。
- 它们通过`CombatTargetAdapter`注册到统一实体查询和伤害管线。
- Boss行为使用C#有限状态机，不把核心规则散落在Animation回调中。
- 动画回调可以发出“表现到达攻击点”的信号，但伤害权限和重复命中由战斗系统控制。
- Boss元素状态与普通怪使用相同的`ElementResolver`。
- 冻结在Boss上转换为失衡值，不复制第二套元素规则。

## 16. 空间网格

### 16.1 固定网格

竞技场边界已知，因此使用固定二维Uniform Grid而不是动态树：

- 网格单元大小根据常见攻击半径和敌人体型选择。
- 每个物理Tick重建或增量更新敌人所在单元。
- 每个单元容器预留容量并复用。
- 查询结果写入调用者提供的可复用缓冲区。
- Debug模式可以绘制网格、实体ID和查询范围。

### 16.2 查询类型

- 圆形范围。
- 扇形范围。
- 线段/胶囊扫掠。
- 最近目标。
- 指定范围内密度最高点。
- 链式攻击的最近未命中目标。

禁止在热路径使用：

- `GetTree().GetNodesInGroup()`扫描全部敌人。
- 为每次法术创建临时`Area3D`。
- 对每个投射物调用大量PhysicsServer查询。
- 每次查询新建`List<T>`或LINQ结果。

## 17. 战斗管线

### 17.1 总体流程

```text
SpellScheduler
→ 创建攻击/投射物状态
→ SpatialGrid命中查询
→ DirectDamage
→ ApplyElement
→ ResolveReaction
→ EffectCommandBuffer
→ Statistics
→ PresentationEvents
```

模拟结果与表现解耦。表现事件可以合并或丢弃，但不能改变伤害结果。

### 17.2 伤害上下文

```csharp
public enum ElementType : byte
{
    None,
    Fire,
    Frost,
    Lightning
}

[Flags]
public enum DamageFlags : byte
{
    None = 0,
    CanTriggerReaction = 1 << 0,
    IsReactionDamage = 1 << 1,
    ForceReactionCheck = 1 << 2
}

public readonly struct DamageContext
{
    public readonly EntityHandle Source;
    public readonly EntityHandle Target;
    public readonly StringName SourceSpellId;
    public readonly float BaseDamage;
    public readonly ElementType Element;
    public readonly int ElementStacks;
    public readonly DamageFlags Flags;
    public readonly int ChainDepth;
}
```

元素催化使用`ElementType.None + ForceReactionCheck`，不添加新元素，只检查目标现有状态并使用催化阈值。

### 17.3 伤害顺序

```text
基础伤害
→ 固定加成
→ 同类百分比加成
→ 独立乘区
→ 目标抗性/减伤
→ 最小伤害限制
→ 生命扣除
```

纵向切片不加入暴击，减少早期平衡维度。

### 17.4 效果命令缓冲

深层命中逻辑不能立即递归产生新命中。它向固定容量、可复用的命令缓冲写入：

```text
ApplyDamage
ApplyElement
ApplyControl
SpawnArea
SpawnProjectile
ChainToTarget
EmitPresentationEvent
```

- 命令按确定顺序消费。
- 同一效果链记录`ChainDepth`，初始硬上限为4。
- 单Tick命令具有硬上限。
- 达到上限时记录错误、对局种子、时间和来源构筑，避免无限递归。

## 18. 元素系统

### 18.1 状态

```csharp
public struct ElementState
{
    public int Stacks;
    public float RemainingDuration;
    public float InternalCooldown;
}
```

普通怪直接内联三个状态字段。Boss适配器也向`ElementResolver`暴露相同接口。

### 18.2 无序反应键

```csharp
public readonly struct ReactionKey : IEquatable<ReactionKey>
{
    public ElementType First { get; }
    public ElementType Second { get; }

    public ReactionKey(ElementType a, ElementType b)
    {
        First = a < b ? a : b;
        Second = a < b ? b : a;
    }
}
```

| 元素组合 | 配置ID | 产品名称 |
|---|---|---|
| Fire + Frost | reaction.thermal_fracture | 热裂 |
| Fire + Lightning | reaction.plasma_burst | 等离子爆发 |
| Frost + Lightning | reaction.conductive_crystal | 导电冰晶 |

### 18.3 ReactionDefinition

至少包含：

- 两种元素。
- 自动触发最低层数。
- 催化触发最低层数。
- 两种元素的消耗规则。
- 基础倍率和每消耗层倍率。
- 范围、传播数量和控制参数。
- 目标内部冷却。
- 优先级。
- 表现事件ID、音效和特效资源。

### 18.4 普通解析顺序

1. 验证来源和目标Handle。
2. 施加元素层数并刷新持续时间。
3. 查找与其他状态形成的候选反应。
4. 使用自动阈值过滤。
5. 排除内部冷却中的反应。
6. 按优先级和配置ID稳定排序，选择最多一个。
7. 消耗层数并写入效果命令。
8. 发布统计和表现事件。

### 18.5 催化解析

1. 不添加新元素。
2. 在目标现有状态之间查找候选。
3. 使用较低的催化阈值。
4. 伤害按实际消耗层数计算。
5. 每个目标最多结算一次主要反应。

### 18.6 防止无限连锁

- 普通法术默认带`CanTriggerReaction`。
- 反应伤害带`IsReactionDamage`，默认不带`CanTriggerReaction`。
- 反应生成的状态不能在同一命令链再次触发主要反应。
- 同一目标和同一Tick最多触发配置允许的主要反应数。
- 所有链式效果受`ChainDepth`和命令缓冲上限保护。

## 19. 法术系统

### 19.1 SpellDefinition

包含：

- 稳定ID、名称、描述和图标。
- 基础伤害、冷却、范围、数量、速度和持续时间。
- 目标选择类型。
- 施法模式与投射物/区域参数。
- 默认元素与基础层数。
- 内容标签。
- 升级分支引用。
- 模型、材质、特效和音频表现引用。

### 19.2 调度

`SpellLoadout`保存最多四个`SpellRuntime`：

- 更新冷却。
- 到期后请求目标。
- 创建攻击状态。
- 重置冷却。

不得为每个装备法术创建持续处理的独立Node。

### 19.3 标签

使用`StringName`常量，不在热路径反复创建字符串：

```text
element.fire
element.frost
element.lightning
spell.projectile
spell.area
spell.orbit
upgrade.behavior.split
upgrade.behavior.chain
```

### 19.4 属性聚合

```text
(Base + Sum(Add))
× (1 + Sum(AddPercent))
× Product(Multiply)
→ Override（按明确优先级）
```

行为升级使用显式枚举、Tag或分支配置，不伪装成含义不清的数值字段。

## 20. 投射物系统

### 20.1 中央状态

```csharp
public struct ProjectileState
{
    public int Generation;
    public bool Active;
    public Vector2 Position;
    public Vector2 PreviousPosition;
    public Vector2 Velocity;
    public float Radius;
    public float RemainingLife;
    public int RemainingPierces;
    public DamagePayload Payload;
    public ProjectileVisualKind VisualKind;
}
```

- 使用预分配数组和Free List。
- 单个`ProjectileSystem._PhysicsProcess()`批量推进。
- 用上一位置到新位置的线段/圆扫掠避免高速穿透。
- 通过空间网格获取潜在目标。
- 穿透记录使用固定小缓冲或命中代数，禁止每颗弹创建`HashSet`。

### 20.2 表现

- 同类简单投射物使用`MultiMeshInstance3D`。
- 复杂轨迹可以使用池化`Node3D`表现，但模拟仍由中央系统权威控制。
- 纯装饰尾迹使用GPUParticles3D，不参与命中。
- 视觉实例数量不足时，允许降低低优先级投射物表现，但模拟必须保持。

### 20.3 持续区域

- 按固定间隔结算，不逐帧造成伤害。
- 使用空间网格查询。
- 同类区域设置数量上限。
- 达到上限时按配置合并、刷新或替换最旧区域。

## 21. 经验物与掉落

- 经验能量使用中央`PickupSystem`保存位置、价值、磁吸状态和速度。
- 使用MultiMesh批量显示，不为每颗经验创建独立`Area3D`。
- 玩家进入磁吸范围后，PickupSystem批量更新吸附。
- 接近玩家的经验可以合并价值，降低实例数量。
- 地图上经验达到上限后，合并到附近经验或提高单个价值，不继续无限生成。

## 22. MultiMesh表现层

### 22.1 普通敌人

- 每种普通敌人原型至少拥有一个`MultiMeshInstance3D`。
- 每个物理Tick后批量写入活跃实例Transform。
- 实例颜色和Custom Data编码受击、元素状态、动画相位等表现参数。
- 简单移动/摆动优先在Shader中根据实例数据完成。
- 玩家、Boss和高价值精英保留独立骨骼动画节点。

### 22.2 分区

MultiMesh以整体AABB参与可见性判断，不能单独剔除每个实例。因此：

- 当前单张开放竞技场可以先按敌人类型分组。
- 若地图扩大，将同类型MultiMesh按空间区块拆分。
- 显式维护正确的Custom AABB。
- 不创建一个覆盖无限地图的全局MultiMesh。

Godot官方说明MultiMesh能够以较低API开销绘制大量实例，但整体进行可见性判断。参考：[MultiMesh优化](https://docs.godotengine.org/en/stable/tutorials/performance/using_multimesh.html)。

## 23. 对象池

需要池化的Node：

- Boss/精英场景实例。
- 少量复杂投射物表现。
- 持续区域表现。
- 高频音频播放器。
- 高频GPUParticles3D节点。
- 伤害数字Label。
- 地图事件临时表现。

普通怪、普通投射物和经验不走Node池，而走数组Free List。

池化接口：

```csharp
public interface IPoolable
{
    void OnAcquireFromPool();
    void OnReleaseToPool();
}
```

归还时：

- 停止处理和物理处理。
- 停止粒子、动画和音频。
- 清除Timer、Tween和事件订阅。
- 清空目标引用和运行状态。
- 从空间网格及战斗注册表注销。
- 隐藏或移出活动场景分支。

## 24. 波次与事件

### 24.1 WaveDirector

```text
RunClock
→ Threat Curve
→ 可用敌人集合
→ 敌人成本
→ 生成批次
→ 精英/事件修正
```

每种敌人配置：

- 生成成本。
- 出现时间区间。
- 同时存在上限。
- 批次大小和间隔。
- 允许的事件和首领阶段。

### 24.2 生成规则

- 出现在镜头外。
- 与玩家保持最小安全距离。
- 不在障碍或地图边界外生成。
- 达到实体硬上限时停止生成，不无限积压请求。
- 首领阶段使用独立增援配置。

### 24.3 事件生命周期

```text
Inactive
→ Telegraphing
→ Active
→ Succeeded / Failed
→ Cleanup
```

地图事件通过公开命令修改WaveDirector压力，不直接操作敌人内部数组。

## 25. 随机数与复现

每局生成并记录根`RunSeed`，派生独立`RandomNumberGenerator`：

- `WaveRandom`
- `UpgradeRandom`
- `SpawnPositionRandom`
- `GameplayProcRandom`
- `PresentationRandom`

派生种子使用项目自定义的稳定整数哈希，禁止使用.NET字符串`GetHashCode()`作为跨进程稳定种子。

视觉和音效随机不得推进玩法随机流。日志、结算和错误报告包含根种子。

相同版本、相同Resource、相同根种子下，升级序列和波次结构应可复现；纵向切片不承诺完整输入回放或跨平台浮点完全一致。

## 26. 升级系统

候选生成顺序：

1. 收集满足前置Tag和法术条件的升级。
2. 排除满级、冲突分支和缺失来源的升级。
3. 根据空法术槽决定是否允许新法术。
4. 根据当前构筑计算权重。
5. 使用Upgrade随机流无放回抽取三个选项。
6. 不足三个时使用合法通用升级补足，并记录配置错误。

升级界面打开时：

- RunController进入`LevelUp`。
- RunClock和全部模拟停止。
- UI取得输入焦点。
- 选择后立即重建相关法术属性。
- 恢复进入界面前的对局状态。

## 27. UI

### 27.1 场景

```text
main_menu.tscn
run_hud.tscn
upgrade_screen.tscn
pause_menu.tscn
run_results.tscn
settings_screen.tscn
```

### 27.2 数据边界

- UI读取不可变快照或只读接口。
- UI不直接修改敌人、法术数组和Run状态字段。
- 用户操作通过命令提交给RunController或UpgradeSystem。
- HUD避免订阅每次普通命中，高频数据按帧读取聚合快照。
- 伤害数字池化并按位置/时间合并。
- 全部菜单支持手柄焦点导航。

### 27.3 表现优先级

```text
玩家危险预警
> 玩家受击/低生命
> Boss和精英技能
> 主要元素反应
> 普通命中
> 装饰性特效
```

达到表现预算时从低优先级开始减少，不改变战斗模拟。

## 28. VFX、材质与音频

- 大范围表现优先使用GPUParticles3D。
- 高频特效节点池化，不反复Instantiate和QueueFree。
- 三种反应拥有不同颜色、轮廓、运动和音效。
- 尽量复用Material和Shader配置，使用实例参数表达元素差异。
- 避免大量透明层叠造成Fill Rate瓶颈。
- 限制动态灯、阴影和全屏后处理数量。
- 高频命中音效限制并发和播放频率。
- 屏幕震动通过单一CameraFeedback服务混合并遵守设置。

Godot官方建议在大量3D对象中复用材质、控制透明对象、动态灯光和阴影，并在重复对象场景使用实例化。参考：[优化3D性能](https://docs.godotengine.org/en/latest/tutorials/performance/optimizing_3d_performance.html)。

## 29. 存档

### 29.1 文件

```text
user://settings.json
user://profile.json
user://last_run.json
```

档案至少包含：

```text
SchemaVersion
TutorialFlags
BestSurvivalTime
BestKillCount
BossDefeated
LastRunSummary
```

### 29.2 DTO边界

- 存档使用纯C# DTO，不直接序列化Node、Resource或SceneTree。
- 保存内容只包含稳定ID和基础数据。
- Resource由ContentCatalog通过ID重新解析。
- JSON序列化设置集中管理，禁止不同服务自行选择格式。

### 29.3 写入安全

- 文件包含显式`SchemaVersion`。
- 写入临时文件并刷新成功后替换正式文件。
- 加载失败时保留损坏文件副本并回退默认数据。
- 设置变化可延迟合并保存；结算时保存对局记录。
- 文件IO不在战斗关键帧同步执行大量数据。
- 每个正式Schema提供从上一个版本迁移的函数。

## 30. 内存和分配规则

C#热路径目标：预热后每个物理Tick不产生常态托管分配。

禁止在敌人、投射物、空间查询和伤害循环中：

- LINQ。
- 闭包和捕获Lambda。
- 字符串拼接或插值日志。
- 每帧新建`List`、`Dictionary`、数组和委托。
- 频繁装箱枚举或结构体。
- 反复读取未缓存的Node路径。

允许：

- 初始化、加载和升级界面等非热路径进行小量分配。
- 通过对象池或`ArrayPool<T>`复用临时大缓冲。
- 使用Profiler证明无问题后保留可读性更好的非热路径实现。

## 31. 多线程边界

纵向切片初版单线程模拟。原因：

- 目标规模首先应由中央数组和算法解决。
- 大量Godot对象和渲染API只能安全地在主线程使用。
- 多线程会增加竞态、同步和复现难度。

只有Profiler确认纯数据计算为瓶颈后，才允许把以下内容放入Worker：

- 敌人移动意图计算。
- 大批量空间网格构建。
- 不接触GodotObject的纯数值步骤。

Worker输出命令或数组结果，由主线程应用到MultiMesh和Node。禁止后台线程访问SceneTree。

## 32. 性能预算

### 32.1 初始目标

第一次性能验收前指定一台真实目标测试机器。

| 指标 | 目标 |
|---|---:|
| 分辨率 | 1920×1080 |
| 帧率 | 稳定60 FPS |
| 总帧时间 | 16.67 ms以内 |
| 活跃普通敌人 | 500 |
| 活跃投射物 | 800–1000 |
| 活跃持续区域 | 50 |
| 模拟CPU预算 | 6 ms初始目标 |
| GPU预算 | 12 ms初始目标 |
| 热路径托管分配 | 0 B/物理Tick目标 |
| 压力运行 | 连续20分钟无持续内存增长 |

性能验收使用Windows Release Export，不以编辑器帧率为最终结论。

### 32.2 节点预算原则

- 普通敌人数量增长不能线性增加SceneTree节点数量。
- 普通投射物和经验数量增长不能创建等量Node。
- 玩家、Boss、精英、地图事件和少量复杂表现可以使用独立Node。
- 任何新增逐帧Node都需要说明必要性。

### 32.3 渲染预算

- 普通怪尽量使用低面数网格和共享材质。
- 大量普通怪不使用完整Skeleton3D动画实例。
- 动态阴影优先保留给玩家、Boss和关键场景对象。
- 粒子系统设置数量、生命周期和可见性上限。
- 透明材质面积和层叠数量纳入GPU测试。

### 32.4 Profiling

- Godot内置Profiler用于引擎、帧、物理、渲染和GDScript相关指标。
- C#脚本使用Rider Profiler、dotTrace或其他.NET Profiler。
- 使用`Performance.GetMonitor()`采集长期压力数据。
- 每个里程碑保存固定场景的基准结果。
- 先确认CPU、GPU或分配瓶颈，再决定优化方式。

Godot内置Profiler目前不直接分析C#脚本函数，官方建议使用外部.NET分析工具。参考：[Godot Profiler](https://docs.godotengine.org/en/4.7/tutorials/scripting/debug/the_profiler.html)。

## 33. GDExtension迁移条件

只有同时满足以下条件才建立C++ GDExtension任务：

1. Windows Release Export无法达到性能预算。
2. Profiler确认瓶颈位于纯C#模拟，而不是渲染、物理或算法错误。
3. 中央数组、空间网格、缓存复用、MultiMesh和分频优化后仍不满足。
4. 已有自动测试保护伤害、元素、随机和波次结果。
5. 待迁移模块具有稳定、窄小的数据接口。

优先迁移：

- 空间网格批量构建与查询。
- 敌人移动/分离计算。
- 投射物批量运动与扫掠。

不优先迁移UI、存档、内容配置、玩家控制和Boss状态机。

Godot官方也建议仅在确有大规模计算需求时使用C++ GDExtension处理性能热点，其余逻辑继续使用脚本语言。参考：[Godot常见问题](https://docs.godotengine.org/en/stable/about/faq.html)。

## 34. 测试策略

### 34.1 NUnit纯逻辑测试

把不依赖SceneTree的规则放在可直接测试的C#类中，覆盖：

- 三个无序ReactionKey。
- 元素层数添加、过期、消耗和内部冷却。
- 自动阈值与催化阈值。
- 单次命中最多一个主要反应。
- 反应伤害默认不能继续触发反应。
- 多候选反应的稳定优先级。
- Boss冻结转失衡规则。
- 属性聚合顺序。
- 升级前置、冲突、权重和无重复抽取。
- 固定种子的升级和波次结果。
- 存档序列化与Schema迁移。

### 34.2 Headless集成测试

使用专用测试入口和Godot Headless运行：

- MainMenu → Run → Results → MainMenu。
- 暂停和升级时所有模拟停止。
- 池化节点复用后无状态、信号和Tween残留。
- EntityHandle代数阻止旧命令命中新实体。
- 投射物穿透不会重复命中过期实体。
- 胜利、失败和重开不会保留上局服务。
- 键鼠和手柄动作映射完整。

### 34.3 压力测试

`performance_arena.tscn`支持：

- 逐步生成至500敌人。
- 同时运行1000投射物和50区域。
- 高频触发三类反应。
- 自动运行至少20分钟。
- 输出平均帧、低分位帧时间、分配、数组容量、命令峰值和内存趋势。

### 34.4 冒烟命令

M0建立脚本完成：

- Headless导入和C#编译。
- 内容验证。
- 单元测试。
- Headless集成场景。
- Windows Debug Export。

具体命令以安装的Godot 4.7.1 CLI参数实测后写入仓库脚本，不在文档中假设本机可执行文件路径。

## 35. 内容验证

`ContentValidator`在编辑器工具和Headless测试中检查：

- 内容ID唯一且非空。
- 必填Resource引用有效。
- 数值不是NaN并处于合理范围。
- 反应两种元素不同且组合不重复。
- 自动阈值不低于催化阈值。
- 法术升级分支无循环引用。
- Required Tags与Blocked Tags不冲突。
- 波次敌人引用有效且成本大于0。
- MultiMesh容量满足配置目标。
- 所有Windows导出依赖资源被正确包含。

错误必须阻止里程碑构建，警告必须在构建报告中列出。

## 36. 调试工具

Debug构建提供可切换调试HUD：

- FPS、物理帧和渲染帧时间。
- 普通敌人、Boss/精英、投射物、区域和经验数量。
- 数组容量、Free List、池已使用和峰值。
- 空间网格、实体Handle和攻击查询绘制。
- RunSeed、对局时间和随机流摘要。
- DPS、各法术伤害和反应次数。
- Wave威胁预算和当前生成成本。
- 无敌、时间倍率、快速升级和强制元素状态。

调试HUD、作弊和详细追踪不进入Release Export。

## 37. 日志与错误处理

日志类别：

```text
Core
Combat
Elements
Spells
Enemies
Run
Content
Save
Performance
```

- 内容验证失败时阻止开始对局。
- 数组容量耗尽、命令超限和重复ID记录错误。
- 高频错误限流，避免日志造成额外卡顿。
- 对局级错误附带Build版本、场景、种子和对局时间。
- Release版本关闭高频Verbose日志。
- 不以异常作为普通战斗分支控制流。

## 38. C#编码规范

- 类型、公开成员和属性使用`PascalCase`。
- 私有字段使用`_camelCase`。
- 常量和静态`StringName`集中声明。
- 开启Nullable Reference Types。
- Godot生命周期方法保持薄入口，核心规则放入普通C#类。
- `_Process()`用于表现和UI；权威模拟集中在少数`_PhysicsProcess()`入口。
- 在`_Ready()`缓存节点引用，不在热路径反复`GetNode()`。
- 使用类型化Signal或C#事件，并在退出树/回池时解除订阅。
- `async`任务必须处理场景卸载与取消。
- 不在后台线程访问SceneTree、Resource和渲染对象。
- `QueueFree()`不作为高频对象生命周期方案。
- 不在热路径使用`CallDeferred()`隐藏时序问题。
- 公共API和复杂算法注释解释原因，不复述代码表面行为。

## 39. 导出

### 39.1 Export Preset

- `Windows Debug`
- `Windows Release`

### 39.2 规则

- 导出架构x86_64。
- `export_presets.cfg`提交非敏感配置。
- 签名密钥、Steam凭据和其他秘密不进入仓库。
- Release关闭远程调试和开发作弊。
- 每次里程碑生成独立导出包并记录Git提交、Godot版本和资源版本。
- 性能验收在Release包中执行。

Godot Windows导出会生成可执行文件和项目数据包，官方参考：[Exporting for Windows](https://docs.godotengine.org/en/stable/tutorials/export/exporting_for_windows.html)。

## 40. 完成定义

一个玩法功能同时满足以下条件才算完成：

- 符合产品文档和任务验收标准。
- 核心逻辑采用C#，没有为普通怪或投射物新增独立逐帧节点。
- 内容Resource通过验证。
- 核心规则具有NUnit测试。
- 关键流程具有Headless集成测试或明确回归步骤。
- 固定压力场景没有不可接受的性能退化。
- 热路径无新增常态分配。
- 具有必要的统计、调试显示和错误日志。
- 键鼠和手柄均可操作。
- UI、音频和可访问性设置得到尊重。
- 受影响的产品或技术规则已更新文档。

## 41. 开发里程碑

### M0：Godot工程基础

- 创建Godot 4.7.1 .NET项目和C#工程。
- 配置Forward+、InputMap、碰撞层和Windows导出。
- 创建AppRoot、MainMenu、Run和PerformanceArena场景。
- 建立Settings、Save、ContentCatalog和SceneRouter。
- 建立RunController、根种子、日志和调试HUD骨架。
- 建立NUnit与Headless冒烟测试。

退出条件：主菜单能进入空白Arena并安全返回；Windows Debug和Release均能导出。

### M1：基础战斗

- 玩家移动、闪避、生命和受击。
- EnemyState数组、EntityHandle、Free List和中央更新。
- 空间网格和普通敌人MultiMesh表现。
- 奥术飞弹和中央投射物系统。
- 经验系统和升级界面框架。

退出条件：300个普通敌人下可持续战斗，升级流程完整。

### M2：元素核心

- 火、冰、雷状态。
- 三种反应、双阈值和防递归。
- 火球、冰锥和链式闪电。
- 元素催化主动技能。
- 元素调试视图和自动测试。

退出条件：固定测试场景能稳定、可复现触发全部三种反应。

### M3：构筑与内容

- 五种法术和每个法术五级分支。
- 升级前置、权重、冲突和刷新。
- 六种普通敌人和两种精英。
- WaveDirector和两类地图事件。
- 经验MultiMesh、表现预算和初步平衡。

退出条件：完成无Boss的12分钟对局，至少形成三种明显不同构筑。

### M4：完整纵向切片

- 灰烬巨像Boss。
- 胜负、结算、统计和版本化存档。
- 完整HUD、设置、键鼠与手柄提示。
- GPUParticles3D、音效、镜头和基础美术替换。

退出条件：满足产品文档功能验收。

### M5：性能与试玩

- Windows Release压力测试。
- 两轮外部玩家测试。
- 修复阻断问题，调整数值和可读性。
- 输出纵向切片复盘和下一阶段建议。

退出条件：满足产品体验与性能验收，或形成明确的未通过结论。

## 42. 架构决策记录

| ID | 决策 | 原因 | 重新评估条件 |
|---|---|---|---|
| ADR-001 | Godot 4.7.1 .NET | 当前稳定版本，适合Windows单机和轻量迭代 | 引擎存在阻断性缺陷 |
| ADR-002 | C#单语言核心 | 强类型、数值系统维护和Windows平台适配良好 | 团队无法维护C#或平台范围改变 |
| ADR-003 | 普通怪中央数组模拟 | 避免500个独立节点处理开销 | 目标实体规模显著降低 |
| ADR-004 | 投射物中央模拟 | 避免约1000个Area3D和独立处理 | 玩法必须依赖复杂物理弹道 |
| ADR-005 | MultiMesh批量表现 | 降低重复敌人与投射物绘制开销 | 所有单位必须使用独立骨骼表现 |
| ADR-006 | XZ固定空间网格 | 适合开放竞技场与大量二维范围查询 | 战斗转为复杂立体空间 |
| ADR-007 | Resource静态配置 | 原生、文本友好、适合数据驱动内容 | 需要外部热更新或Mod系统 |
| ADR-008 | 不在首版使用GDExtension | 保持构建和调试简单 | 满足第33节全部条件 |
| ADR-009 | 固定种子拆分随机流 | 复现问题并隔离表现随机 | 需要完整回放时提升确定性 |
| ADR-010 | 三元素无序反应 | 控制教学、平衡和测试矩阵 | 玩家理解稳定且需要更深组合 |

新增关键技术决策继续编号，不覆盖历史理由。

## 43. 文档维护

- 规则、范围或架构变化时提升次版本号。
- 仅修正文案或补充说明时提升修订号。
- 开发新系统前确认产品和技术文档已有对应定义。
- 实现发现文档不可执行时，先记录冲突和建议，再批准变更。
- 每个里程碑结束复查产品验收、性能预算、风险和ADR。
- Godot、.NET SDK、插件或导出配置升级必须记录完整版本和回归结果。
