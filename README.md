# Project Catalyst

Project Catalyst 是一款使用 Godot 4.7.1 .NET 和 C# 开发的单机3D俯视角元素反应类幸存者游戏。

## 技术基线

- Godot 4.7.1 Stable .NET
- .NET SDK 8
- Forward+ / Windows x86_64
- C#中央模拟普通敌人、投射物和经验物
- MultiMesh批量表现

产品和技术规范：

- [产品需求文档](docs/PRODUCT_SPEC.md)
- [Godot技术设计文档](docs/TECHNICAL_DESIGN_GODOT.md)
- [V2整体玩法系统设计](docs/GAME_SYSTEM_DESIGN_V2.md)
- [V2元素化学与反应设计](docs/ELEMENT_CHEMISTRY_DESIGN.md)

## 本地运行

安装Godot 4.7.1 .NET与.NET 8 SDK，然后在Godot编辑器中导入仓库根目录的`project.godot`。

也可以在PowerShell中安装仓库私有的便携工具链：

```powershell
./scripts/bootstrap-tools.ps1
```

导出模板体积较大，只有需要本机Windows导出时才安装：

```powershell
./scripts/bootstrap-tools.ps1 -WithExportTemplates
```

本工作区可以使用便携工具链：

```powershell
$env:DOTNET_ROOT = "$PWD\.tools\dotnet"
$env:PATH = "$env:DOTNET_ROOT;$env:PATH"
& "$PWD\.tools\godot\Godot_v4.7.1-stable_mono_win64\Godot_v4.7.1-stable_mono_win64_console.exe" --editor --path .
```

## 验证

```powershell
./scripts/verify.ps1
```

里程碑范围和完成定义以技术设计文档为准。

## 当前可玩内容（M4）

- WASD移动、Space闪避、鼠标右键元素催化、Esc暂停。
- 敌人持续从竞技场外围生成并追击玩家。
- 初始拥有奥术飞弹，通过升级从火球、冰锥、链式闪电和环绕法球中构筑最多四个法术。
- 每个法术最高5级，3级选择互斥行为分支，5级完成分支进化。
- 火焰造成燃烧伤害，冰霜减速并可冻结，雷电造成短暂硬直。
- 火冰触发热裂、火雷触发等离子爆发、冰雷触发导电冰晶。
- 元素催化使用更低阈值提前引爆范围内的反应；闪避后下一次催化范围扩大。
- 击杀掉落经验能量，靠近后自动吸附。
- 升级时战斗暂停并出现三选一，单局可刷新一次。
- 当前升级包含伤害、冷却、移动、弹速、生命和拾取范围。
- 六种普通敌人和两种精英按四阶段威胁曲线加入战斗。
- 封印裂隙与稳定法阵会改变行进路线，完成后提供经验和高品质升级。
- 生存12分钟后进入“灰烬巨像”Boss战：锥形重击、连续冲锋、余烬危险区和召唤守卫。
- Boss只受冰霜减速而不会被冻结；元素反应会积累失衡值，失衡后进入4秒易伤窗口。
- 胜利与失败均进入正式结算页，展示击杀、承伤、总伤害、法术伤害、元素反应、最终构筑和Seed。
- 设置支持主/音乐/音效音量、分辨率、全屏、质量、帧率、镜头震动、伤害数字和元素辅助。
- 键鼠与手柄提示会按最后使用的设备切换；暂停菜单支持设置和经确认后重新开始。
- 配置、最佳生存时间、最多击杀、Boss完成状态和最后一局结算使用版本化JSON持久化。
- 基础表现包含元素反应、攻击预警、连锁闪电、伤害数字、程序化反馈音和可调镜头震动。
- 调试面板显示帧率、实体数量、击杀和反应统计。

自动验证包含完整升级闭环、三种元素反应、28项常规内容数据校验、五种法术构筑规则、八种常规敌人、两类事件、500敌人中央模拟压力检查，以及Boss失衡/易伤、结算和版本化存档闭环。
