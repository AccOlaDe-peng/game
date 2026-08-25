# Project Catalyst — ART_STYLE_GUIDE（美术风格指南）

> 文档编号：CAT-ART-001
> 状态：初版（V1.0），随 Visual Target 视觉评审迭代
> 依据：《Project Catalyst 美术资产设计与实施计划》§25（Visual Bible）
> 本指南是 Project Catalyst 视觉语言的唯一权威来源：**任何新资源必须符合本指南，否则不入库。**

## 1. 总方向

**Stylized Low Poly + 魔导科技（Arcane Technology）+ 元素炼金（Elemental Alchemy）。**

- 整体画面偏暗、冷色环境 + 高饱和、强发光（Emissive）的元素 VFX。
- 视觉层级永远优先于“更炫”：危险预警 > 玩家受击 > Boss/精英技能 > 元素反应 > 玩家主攻击 > 普通敌人攻击 > 装饰特效。
- 目标是「这是同一个世界里的角色和怪物」，而不是多个 Asset Pack 的拼接。

## 2. Color Palette（环境色板）

| 用途 | 色值 | 说明 |
|---|---|---|
| 背景（清屏色） | `#0D1318` (0.05, 0.075, 0.095) | 深蓝黑，VFX 的底 |
| 环境光 | `#4D6B99` (0.30, 0.42, 0.60) energy 0.5 | 冷色补光，压低环境光让元素发光跳出 |
| 太阳光 | `#C7E0FF` 暖白 | 主光，强度 1.1~1.2，带阴影 |
| 雾色 | `#141F2E` (0.08, 0.12, 0.18) | 低密度地面雾（density 0.01, height 6, height_density 0.02） |
| 地面 | 暗绿褐 `#1B2B29` roughness 0.92 | 低反光，不抢视觉 |

色调映射：**Filmic**（tonemap_mode=2）；开启 **Glow**（intensity 0.75, blend=Screen）放大元素发光。

## 3. Element Colors（五元素色板）

**唯一色源：`scripts/presentation/ElementPalette.cs`。** 代码、Shader、UI、图标、文档一律以此为准，禁止各处手写不一致的色值。

| 元素 | 主色 | 辅助/高光 | 表现关键词 |
|---|---|---|---|
| 火 Fire | `#FF3D1A` (1.0, 0.24, 0.10) | 橙黄 (1.0, 0.68, 0.20) | 上升、爆裂、火星、热浪 |
| 水 Water | `#1A8FFF` (0.10, 0.56, 1.0) | 亮青 (0.55, 0.86, 1.0) | 流动、波纹、环形扩散 |
| 风 Wind | `#6BFFB3` (0.42, 1.0, 0.70) | 近白 (0.90, 1.0, 0.95) | 螺旋、气流、切割、高速 |
| 土 Earth | `#CC8C38` (0.80, 0.55, 0.22) | 暗褐 (0.40, 0.28, 0.12) | 岩石、冲击、地裂、厚重 |
| 雷 Lightning | `#FFEB40` (1.0, 0.92, 0.25) | 紫 (0.71, 0.30, 1.0) | 高频闪烁、分叉、电弧 |

附加：
- 冰 Frost `#4DD1FF` (0.30, 0.82, 1.0) — 水+风融合
- 标记 Mark `#E652FF` (0.90, 0.32, 1.0) — 中性机制
- 奥术默认 Arcane `#6BD1FF` (0.42, 0.82, 1.0)

### 元素反应色

- 蒸汽热冲击（火+水）：近白热蒸汽 `#F2F7FF` (0.95, 0.97, 1.0)
- 传导闪电（水+雷）：电金 `#FFF559` (1.0, 0.96, 0.35)

> 反应必须至少改变 Color、Shape、Motion、Sound、Screen Feedback、Particle Density 中的三维度，禁止只换颜色。

## 4. Character Scale（角色比例）

统一以 Godot 场景 1 单位 = 1 米为基准：

| 单位 | 高度（目标） | 说明 |
|---|---|---|
| 玩家（元素行者） | ~1.8 m | 现有 Quaternius Superhero_Female 缩放 0.95 |
| 普通敌人 | ~1.0 m（PresentationTargetHeight） | MultiMesh 按 AABB 高归一化到 1.0 |
| 精英 | 普通敌人 × VisualScale（约 2.0） | 大一圈 + 精英光环 |
| Boss（灰烬巨像） | 显著大于普通怪 | 独立预算 |

## 5. Material Style（材质风格）

- 敌人：Low Poly，**共享材质**，用 MultiMesh 实例色（`VertexColorUseAsAlbedo`）做每个实例的颜色/元素染色，不拆材质。
- 玩家：标准 PBR（metallic 0.15, roughness 0.35），蓝 `#2E9EF2`。
- 元素 VFX：统一 `assets/art/shaders/element_vfx.gdshader`，用参数差异表达五种元素
  （element_color / secondary_color / glow_strength / noise_scale / noise_speed / pulse_speed /
  distortion_strength / dissolve_amount / edge_strength / opacity）。
- 精英材质：`assets/art/materials/elite_material.tres`（品红 `#FF4DD9`，emission 2.4，additive）。
- 不做写实材质；控制透明材质数量，VFX 优先 GPUParticles3D（当前原型用 billboard MultiMesh）。

## 6. Outline Style（描边）

当前阶段：**不启用全场景描边**（低性能成本 + 原型阶段）。用「发光边缘（edge_strength）+ 亮色轮廓」在元素 VFX 上模拟边缘感。后续若需要角色描边，走 Outline Shader 统一处理，禁止各角色各写一套。

## 7. Lighting（光照）

- 单主光（DirectionalLight3D，暖白 0.78,0.88,1.0，energy 1.1，开阴影）。
- 环境光低（energy 0.5）→ 元素发光成为画面亮点。
- 地面雾（height fog）给 Arena 纵深；禁止强体积光（性能）。
- 色调映射 Filmic + Glow(Screen) 是默认输出。

## 8. VFX Brightness（VFX 亮度）

- VFX 使用 additive 混合（`blend_add` / `blend_mode=1`），在暗背景上发光。
- 亮度参考：普通攻击发光 0.8~1.5，元素反应 1.6~2.4，Boss/精英技能最高不超过 3.0。
- Glow 阈值之上才进 Bloom；控制同时存在的透明粒子数量，高频效果走池化。

## 9. UI Style（UI 风格）

- 现用 Kenney「UI Pack – Adventure」九宫格（`panel_brown`/`panel_brown_dark`/`button_*`/`progress_*`）。
- 深色面板 + 白图标（`icons/white/`）。
- HUD：顶部状态条、左下 HP/XP、右下元素催化条（就绪亮青 `#8CF2FF` / 冷却灰蓝 `#738CAD`）、
  元素反应提示文字用对应反应色（见 §3）。
- 第一阶段不制作卡牌独立插画：Icon + Card Frame + Type + Element Color + Cost + Name。

## 10. Icon Style（图标风格）

- 第一阶段继续使用 Kenney Game Icons（CC0），深色 UI 用 white 变体。
- 元素图标用色板主色着色（Future）；当前用图标 + 元素色文字组合传达。

## 11. Environment Style（环境风格）

- 第一张正式 Arena：**元素研究遗迹（Arcane Research Ruins）**。
- 基础资产约 20 个：5 Rocks + 5 Ruins + 3 Pillars + 3 Crystals + 3 Machines + 2 Vegetation + 1~3 Ground Materials。
- 用 Rotate / Scale / Material Variant / Decal / Shader / Lighting 制造变化，禁止大量 Biome。
- 当前 Arena 已含：Quaternius 树/灌木/岩石/蘑菇/木箱/木桶/大锅 + 中央奥术符文地面圈（circle_02 + emission）。

## 12. Screenshot Examples（截图示例）

- 待 Visual Target V1（`docs/art/visual_target_v1.png`）评审后在此挂载基准图。

## 13. 校验清单（新资源入库前）

- [ ] 符合 Stylized Low Poly 方向，非写实
- [ ] 颜色符合 §3 色板（或属于色板合理派生）
- [ ] 不影响战斗信息层级（§1）
- [ ] 有明确 License 且已登记（`assets/licenses/README.md`）
- [ ] 命名符合 `assets/art/README.md` 规范
- [ ] 没有破坏同屏可读性（大规模敌人时依然清晰）
