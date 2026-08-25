# assets/art 目录结构与命名规范

> 依据《Project Catalyst 美术资产设计与实施计划》§19（推荐资源目录）与 P0「整理 assets」。
> 所有美术资源必须遵守本规范；新增资源时先确认 License（见 `assets/licenses/README.md`）。

## 目录结构

```text
assets/art/
├── animations/            # 动画：UAL1_Standard.glb（Quaternius 玩家）、mixamo/（12 条待重定向）
├── characters/            # 角色模型：elementalist/（Superhero_Female 元素行者）
├── enemies/
│   ├── monsters/          # 通用敌人：big/（大体型）、blob/（软体）、flying/（飞行）
│   └── orc/               # 早期原型 Orc（保留参考，不再新增）
├── environment/
│   ├── nature/            # 自然物：树、灌木、岩石、蘑菇
│   └── props/             # 道具：木箱、木桶、大锅
├── icons/                 # Kenney 图标：black/、white/（深色 UI 用白变体）
├── materials/             # 材质：elements/（五元素）、elite_material.tres（精英）
├── particles/             # Kenney 粒子贴图（80 张透明集，additive VFX 用）
├── shaders/               # Shader：element_vfx.gdshader（元素共享 VFX Shader）
├── ui/                    # UI 九宫格贴图：panel_*、button_*、progress_*
├── vfx/                   # 预留：运行时 VFX 场景 / GPUParticles（当前 VFX 由代码 + particles 贴图生成）
└── weapons/               # 预留：武器模型（Pulse Rifle / Ricochet Disc / Elemental Mine）
```

### 敌人资源映射（EnemyArchetype → 模型）

| 角色 | 模型 | 文件 |
|---|---|---|
| Swarmer 群行者 | GreenBlob | `monsters/blob/GreenBlob.gltf` |
| Hunter 猎手 | Cat | `monsters/blob/Cat.gltf` |
| Heavy 重装 | Yeti | `monsters/big/Yeti.gltf` |
| Caster 施法者 | Wizard | `monsters/blob/Wizard.gltf` |
| Exploder 自爆 | GreenSpikyBlob | `monsters/blob/GreenSpikyBlob.gltf` |
| Summoner 召唤者 | Ghost | `monsters/flying/Ghost.gltf` |
| EliteCharger 冲锋精英 | Dragon | `monsters/flying/Dragon.gltf` |
| ElementGuard 元素守卫 | Alien | `monsters/big/Alien.gltf` |
| BossAshenColossus 灰烬巨像 | MushroomKing | `monsters/big/MushroomKing.gltf` |

> 精英不单独制作模型：共用普通模型 + 缩放 + 精英光环（`elite_material.tres`）。

## 命名规范

- 目录名、资源名一律 `snake_case`，禁止中文与空格。
- 角色 / 敌人模型：`snake_case.gltf`；贴图：`T_<Name>_<Type>.png`
  （Type：`BaseColor` / `Normal` / `Roughness` / `ORM`）。
- 材质：`<name>_material.tres`；元素材质：`assets/art/materials/elements/<element>.tres`。
- Shader：`<name>.gdshader`，统一放在 `assets/art/shaders/`。
- 粒子贴图 / 图标：沿用来源包原生文件名（`circle_01.png`、`gear.png`），不重命名。
- 动画：`<动作名>.fbx`，Mixamo 文件保留 Mixamo 原名。

## 规则

- 不做：给普通怪增加复杂 Skeleton、制作几十种装饰粒子、为每个技能单独写 Shader。
- 优先：共用 Shader + 参数差异、共用动画、材质变体（Scale / Tint / Variant）代替新资产。
