# 游戏美术替换指南

本文对应当前首批公开素材。素材只用于建立可运行的替换样板，不代表广陵最终美术方向。

## 已准备内容

- Kenney Fantasy Town Kit 的 12 个建筑与街道模块。
- Poly Haven 的灰泥墙、旧木板和陶瓦三套 2K PBR 材质。
- 可直接运行的 Unity URP 材质生成与预览预制体生成工具。
- 原始包、许可证、来源、校验值和素材清单。

![第三方素材导入预览](screenshots/art-import-showcase.png)

![灰盒与美术替换对比](screenshots/art-replacement-comparison.png)

## 首次生成

在 Unity 中打开 Demo02 工程，等待导入完成，执行：

`Demo02 > Art > Setup Third-Party Samples`

工具会完成以下操作：

1. 设置贴图的 sRGB、法线和重复平铺参数。
2. 从 Poly Haven ARM 贴图生成 URP 所需的遮挡与金属/光滑度遮罩。
3. 生成三套项目材质和 Kenney 图集材质。
4. 生成 `ART_Sample_FantasyKit` 预览预制体。
5. 生成 `ArtImportShowcase` 测试场景。

输出位置：

```text
Assets/Demo02/Art/Materials/ThirdPartySamples/
Assets/Demo02/Art/Environment/Guangling/Prefabs/
Assets/Demo02/Scenes/Tests/ArtImportShowcase.unity
Assets/Demo02/Scenes/Tests/ArtReplacementComparison.unity
```

生成后可执行 `Demo02 > Art > Validate Third-Party Samples`，检查模型、
URP Shader、Base Map、法线、遮挡、金属/光滑度引用、样板预制体和预览场景。

`ART_Replacement_Building_8x6.prefab` 是一个已验证的替换样板：

- 根节点下分离 `Visuals`、`Colliders`、`Gameplay` 和 `Metadata`。
- 外来模型只进入 `Visuals`。
- `Colliders` 保留 6×6×8 米街面建筑占位尺寸。
- 墙体模块按 3 米层高、4 米面宽重新组合。
- 材质、屋顶、立柱和灯笼均由项目自有材质引用。

实际替换时复制此预制体，保留碰撞与玩法结构，再按目标灰盒尺寸调整
`Visuals`。不要把原有玩法脚本挂到第三方模型子节点上。

## 场景内安全替换

选中灰盒中的 `LeftBuilding_*` 或 `RightBuilding_*`，执行：

`Demo02 > Art > Apply Building Replacement To Selection`

工具会隐藏旧长方体外观和同名前缀的灰盒立面细节，挂上建筑替换预制体，
但保留原建筑的碰撞体、脚本和场景对象。右侧建筑会自动旋转朝向街道。

执行 `Demo02 > Art > Remove Building Replacement From Selection` 可恢复原灰盒。
两种操作均支持 Unity Undo。

## 替换一个灰盒对象

1. 复制目标场景到测试场景，或在 Unity 中临时隐藏待替换对象。
2. 保留原对象的碰撞体、触发器、脚本和场景 ID。
3. 将新的视觉模型放到 `Visuals` 子节点下。
4. 删除旧视觉 Mesh，但不要删除玩法根节点或碰撞体。
5. 以主角高度校准比例，并检查原点、朝向、门洞和层高。
6. 只替换一个对象，保存前后截图。
7. 在 `SourceAssets/ThirdParty/ASSET_MANIFEST.csv` 中更新替换对象和修改记录。

推荐结构：

```text
Building_A/
├─ Visuals/
├─ Colliders/
├─ Gameplay/
└─ Metadata/
```

## 材质约定

- 灰泥墙使用 `MAT_PolyHaven_GreyPlaster02`。
- 梁柱、门板和旧木结构使用 `MAT_PolyHaven_OldPlanks02`。
- 瓦面使用 `MAT_PolyHaven_ClayRoofTiles02`。
- Kenney 样板模型使用 `MAT_Kenney_FantasyTownAtlas`。
- 项目自有材质和预制体放在 `Assets/Demo02/Art`。
- 第三方原始 FBX、PNG 和 JPG 放在 `Assets/ThirdParty`，不直接修改。

## 提交前检查

```powershell
git lfs install
pwsh demo02/tools/validation/validate-art-assets.ps1
git status --short
```

确认没有 ZIP 被直接放进 Unity `Assets`，没有来源不明的图片，没有未经记录的模型修改。

