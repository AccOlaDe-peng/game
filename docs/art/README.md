# docs/art — 美术交付物

本目录存放美术评审需要的图片输出。

## Visual Target V1

目标文件：`docs/art/visual_target_v1.png`（依据《美术资产设计与实施计划》§24）。

产出步骤：

1. 用 **Godot 编辑器**（非 headless）打开 `scenes/tests/visual_target.tscn` 并运行。
2. 场景会自动：生成 40 个敌人（群行/猎手/重装/施法/自爆/召唤/守卫）+ 1 个冲锋精英，
   从玩家位置扇形齐射五元素投射物，数秒后朝敌群补一发「水→火」定向连击触发蒸汽热冲击。
3. 在动作清晰的一帧按截图（或暂停后截图），保存为 `visual_target_v1.png`。
4. 更新 `docs/ART_STYLE_GUIDE.md` §12（Screenshot Examples），挂载该图。

> headless（命令行）无法渲染截图；此步骤必须在编辑器中人工完成。
