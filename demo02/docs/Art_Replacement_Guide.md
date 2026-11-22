# 游戏美术替换指南

本文记录当前可运行的公开素材替换流程。正式默认环境使用 Quaternius
Medieval Village MegaKit 中现成的模块拼装，不手工制作建筑模型。

## 当前默认：Quaternius 中世纪村庄

- 来源：https://quaternius.com/packs/medievalvillage.html
- CC0 发行镜像：https://github.com/J-Ponzo/gltf-medieval-village-megakit
- 许可证：CC0 1.0，允许个人、商用、修改和再分发。
- 导入范围：31 个墙、门、窗、百叶窗、屋顶、老虎窗、挑檐和街道道具模块。
- 组合结果：三套 6×8 米建筑外壳，分别为灰泥、砖石和木构立面。
- 兼容策略：原建筑的碰撞体、交互、任务与场景 ID 保持不变，只替换视觉。

![Quaternius 主街](screenshots/quaternius-main-street.png)

![Quaternius 独立样板](screenshots/quaternius-village-pilot.png)

重现完整环境：

1. 在 Unity 中打开 `Demo02 > Art > Quaternius > Rebuild Environment Kit`。
2. 执行 `Demo02/Rebuild Greybox Scene`，将三套外壳接入全部街区。
3. 打开 `Assets/Demo02/Scenes/Tests/QuaterniusVillagePilot.unity` 检查独立样板。
4. 执行 `Demo02 > Art > Quaternius > Capture Main Street Screenshot` 生成对比图。
5. 执行 `Demo02 > Build Windows Demo` 生成可运行版本。

性能处理不是简单降低画质。每个建筑先按材质合并；接入街区时，再按
`OfficialStreet`、`DongguanStreet`、`CaiyiStreet` 三个区域合并为三个
静态网格，每组最多 9 个材质批次，避免几十栋建筑各自提交材质。

同一 1280×720 Windows 实机基准：

| 版本 | 平均 FPS | 1% 低帧 FPS |
|---|---:|---:|
| 1.21 原灰盒回滚版 | 192.6 | 99.5 |
| 1.22 Quaternius 主场景 | 376.3 | 164.7 |
| 1.23 Quaternius 环境与动作 | 375.6 | 200.6 |
| 1.24 现成街景与道具 | 375.3 | 207.2 |

基准文件位于 `docs/benchmarks/`。帧率数据来自窗口正常显示并持续渲染
12 秒的构建，不采用最小化窗口或关闭渲染得到的无效结果。

## Quaternius 动作库

角色的待机、奔跑和跳跃继续使用 Kenney 原配动作；攻击、受击、交互、
拾取和倒地来自 Quaternius Universal Animation Library 免费标准包。

- Quaternius 源骨架：Rigify 风格的 `DEF-*` 骨骼。
- 项目角色骨架：Kenney 的 Mixamo 风格 Generic 骨骼。
- 运行时方案：构建阶段按骨骼全局旋转差离线重定向并烘焙，游戏内不执行
  实时重定向，也不增加 Animator 重定向开销。
- 控制器参数：`Attack`、`Hit`、`Interact`、`PickUp`、`Die`。

![Quaternius 角色动作](screenshots/quaternius-animation-showcase.png)

重新生成动作：

1. 执行 `Demo02/Art/Build Stylized Character Assets`。
2. 执行 `Demo02/Art/Quaternius/Rebuild Retargeted Character Actions`。
3. 执行 `Demo02/Rebuild Greybox Scene`，让新状态进入正式场景构建。

重定向脚本位于
`Assets/Demo02/Scripts/Editor/QuaterniusAnimationLibrarySetup.cs`，输出片段位于
`Assets/Demo02/Art/Characters/Stylized/Animations/`。

## 现成街景与道具

剩余灰盒街景同样只使用现成素材，没有新建模型或绘制贴图：

- Quaternius 木箱、木瓦雨棚、门板告示牌、门框标记和墙体模块。
- Kenney 市场摊位、高树、灯笼，统一使用现有 Fantasy Town 图集材质。
- 院落和仓库灰盒墙通过重复 Quaternius 2 米墙段组合成纹理连续的墙体。
- 拾取物、线索、时层锚点仍保留原脚本、碰撞和隐藏逻辑，视觉节点挂在原
  玩法对象下，因此拾取或切换时不会留下悬空模型。

场景重建入口仍是 `Demo02/Rebuild Greybox Scene`。新增工具位于
`Assets/Demo02/Scripts/Editor/ReadyMadeStreetDressingSetup.cs`。

## 早期第三方样板

以下 Kenney 与 Poly Haven 流程继续保留，作为材质和替换结构的参考。

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

