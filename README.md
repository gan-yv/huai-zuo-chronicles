# 淮左纪事 / Huai-Zuo Chronicles

一款以中国古代城市广陵为舞台的 **3D 时间修复动作冒险** Unity 垂直切片。

玩家调查一起“名字被从册页抹去”的盐引案，在旧街与现世之间穿行，通过战斗、
证词、时空锚点和人物选择决定一条消失街巷是否重新进入城市记录。

**A 3D time-repair action-adventure vertical slice built with Unity 6 and URP.**

![《淮左纪事》广陵街区实机画面](demo02/docs/screenshots/lumine-gameplay.png)

## 项目概览

| 项目 | 内容 |
|---|---|
| 当前版本 | **v1.75.2** |
| 引擎 | Unity 6000.5.7f1、Universal Render Pipeline |
| 语言 | C# |
| 目标平台 | Windows x64 |
| 游戏类型 | 单机、第三人称、动作冒险、叙事调查 |
| 当前内容 | 第一章《消失的盐引》 |
| 项目性质 | 可运行的技术与设计作品集，不是商业发行版本 |

## 核心体验

- **时空调查**：在现世与旧时之间切换，观察被覆盖的建筑、物件和记录。
- **叙事推理**：组合人物证词与物证，指出日期和记忆中的矛盾。
- **动作战斗**：三段连式、见隙反击、角色技能、终结技和敌人破防。
- **队伍切换**：两名实际可玩角色与四席队伍框架。
- **开放街区**：官署前街、盐仓横街、回潮巷、旧仓、东关街、彩衣街和林间小屋。
- **世界事件**：可持久化的街区支线、遭遇增援和结局后自由探索。
- **版本化存档**：自动检查点、三个手动槽、校验和、备份恢复和稳定实体 ID。

## 画面展示

| 探索 | 战斗 |
|---|---|
| ![荧在广陵街区探索](demo02/docs/screenshots/lumine-gameplay.png) | ![战斗锁定与攻击反馈](demo02/docs/screenshots/combat-lock-feedback.png) |

| 地图与快速旅行 | 调查案卷 |
|---|---|
| ![广陵街区地图](demo02/docs/screenshots/map-screen.png) | ![调查案卷界面](demo02/docs/screenshots/journal-screen.png) |

| 纸鸢支线 | 标题界面 |
|---|---|
| ![纸鸢旧信支线](demo02/docs/screenshots/openworld-paper-kite.png) | ![标题界面](demo02/docs/screenshots/title-anime-fish.png) |

## 当前可玩版本

最新版本：[**portfolio-v1.74.4**](https://github.com/gan-yv/huai-zuo-chronicles/releases/tag/portfolio-v1.74.4)

- 所有历史版本及逐版本更新说明：[GitHub Releases](https://github.com/gan-yv/huai-zuo-chronicles/releases)
- 带 Windows 构建附件的版本：以对应 Release 的 Assets 为准
- 当前仓库包含完整的 Unity 工程，可直接打开和运行
- 发布版本遵循语义化版本，内容版本和存档架构版本独立记录

## 快速开始

### 使用 Unity

1. 安装 Unity Hub 和 Unity **6000.5.7f1**。
2. 打开 [demo02/UnityProject](demo02/UnityProject)。
3. 等待首次资源导入与脚本编译完成。
4. 打开 `Assets/Demo02/Scenes/Boot/Boot.unity`。
5. 点击 Play，从标题界面开始游戏。

### 基本操作

| 操作 | 键盘与鼠标 |
|---|---|
| 移动 / 镜头 | `WASD` / 按住鼠标右键 |
| 冲刺 / 跳跃 / 滑翔 | `Shift` / `Space` / 空中再次 `Space` |
| 攻击 / 闪避 | 鼠标左键 / `Ctrl` |
| 技能 / 终结技 | `F` / `R` |
| 调查或交谈 | `E` |
| 时间层 / 世界地图 | `Q` / `M` |
| 案卷 / 证词 / 背包 | `J` / `T` / `B` |
| 暂停 / 帮助 | `Esc` / `F1` |

支持 Input System 键位重绑定和基础手柄操作。

## 工程重点

这个仓库不仅保存可玩内容，也记录从灰盒原型逐步升级为可维护 Unity 工程的过程。

### 程序集与依赖边界

- `Demo02.Runtime`：运行时领域、玩法、世界状态与表现。
- `Demo02.Editor`：内容生成、场景装配、构建、验证和版本审计。
- `Demo02.Tests.EditMode`：按领域组织的自动测试入口。
- 架构审计禁止运行时反向引用编辑器，也禁止直接读取命令行参数。

### 运行时服务

- `GameServices` 提供 Composition Root 和服务契约。
- `IGameFlowState` 隔离暂停与结束状态。
- `IContentProvider` 隔离 Resources 和未来 Addressables 实现。
- `RuntimeLaunchOptions` 统一预览、截图、测试和性能采样参数。

### 数据与内容管线

- 唯一运行时内容目录：`GameContentCatalog`。
- `ChapterDefinition`、`CharacterDefinition`、`WorldEncounterDefinition` 和
  `WorldStreamingRegionDefinition` 统一由稳定 ID 查询。
- 案件、证据、证词、对白、物品和可选目标由 `CaseDefinition` 驱动。
- Editor 内容管线覆盖内容生成、资源预算、场景流式、数据目录和孤儿资产审计。

### 存档与世界状态

- 当前存档架构版本为 **3**。
- SHA-256 payload 校验、临时文件验证、`.bak` 恢复和逐版本迁移。
- 自动存档采用状态签名、防抖和限频检查点。
- 战斗单位、区域、锚点、故事事实、拾取物和街区事件按稳定 ID 持久化。

### UI 与输入

- 主要 HUD 和模态界面已迁移到持久 UGUI。
- 统一模态栈管理界面所有权、输入阻塞和关闭流程。
- 支持键鼠、基础手柄、动态提示文本和键位重绑定。

## 自动验证

当前 EditMode 测试分为：

- `Architecture`：程序集边界、服务容器和版本一致性。
- `Save`：迁移、校验和、备份恢复、槽位和实体 ID。
- `Story`：章节阶段、对白条件、时间层和结局。
- `Combat`：连式、闪避、敌人档案、背包和角色属性。
- `Data`：内容目录、资源预算和跨资产引用。
- `World`：开放世界、流式区域和地图可读性。
- `InputUi`：Input Actions、重绑定、模态栈和 UGUI 层级。

最新完整回归为 EditMode **13/13**、PlayMode **3/3** 通过。

## 仓库结构

```text
.
├─ demo01/                         # 归档的旧 2D 原型与历史验证
├─ demo02/                         # 当前 3D 游戏工程
│  ├─ UnityProject/
│  │  └─ Assets/Demo02/
│  │     ├─ Art/                   # 角色、环境、UI 与第三方资源
│  │     ├─ Audio/
│  │     ├─ Data/                  # ScriptableObject 内容资产
│  │     ├─ Resources/Data/        # 运行时唯一内容目录
│  │     ├─ Scenes/                # Boot、广陵主场景与流式场景
│  │     ├─ Scripts/               # 运行时、编辑器和编辑器专用工具
│  │     ├─ Tests/                 # EditMode 自动测试
│  │     └─ UI/
│  ├─ SourceAssets/                # Blend、GLB、导入报告和源素材
│  ├─ docs/                        # 设计、架构、内容、测试与版本规范
│  ├─ tools/                       # 构建、资源和工程校验工具
│  └─ version.json                 # 单一版本源
├─ CHANGELOG.md                    # 逐版本开发记录
├─ PORTFOLIO.md                    # 作品集说明
└─ CONTRIBUTING.md                 # 目录、分支、提交和资源规范
```

## 文档入口

- [Demo02 项目说明](demo02/README.md)
- [作品集与技术亮点](PORTFOLIO.md)
- [最新发布说明](RELEASE_NOTES.md)
- [代码架构](demo02/docs/Architecture.md)
- [内容管线](demo02/docs/Content_Pipeline.md)
- [存档与输入规范](demo02/docs/Save_And_Input.md)
- [测试说明](demo02/docs/Testing.md)
- [版本规范](demo02/docs/Versioning.md)
- [性能预算](demo02/docs/Performance_Budget.md)
- [完整变更记录](CHANGELOG.md)

## 维护规则

- 每次代码、数据或文档结构修改必须递增版本并更新 `CHANGELOG.md`。
- `demo02/version.json` 是版本单一来源，构建和版本审计会交叉校验。
- 可玩版本创建 annotated tag，并在 GitHub Release 中提供独立更新说明。
- 二进制素材通过 Git LFS 管理。
- 第三方素材必须记录作者、来源、许可证和用途。

详见 [版本规范](demo02/docs/Versioning.md) 和 [贡献规范](CONTRIBUTING.md)。

## 当前限制

- 项目仍是垂直切片，章节、角色和城市内容规模有限。
- 部分界面和场景表现仍使用程序化占位资源。
- 音频、镜头、敌人组合和长期性能仍需要更多实机验证。
- Windows 构建资产只在对应 Release 提供时可用。
- 仓库中原创代码和内容的授权方式尚需单独声明；第三方资源分别遵循其来源许可证。

## 第三方资源

第三方模型、纹理、音乐和工具记录于：

- [ThirdParty README](demo02/UnityProject/Assets/ThirdParty/README.md)
- [ThirdParty CREDITS](demo02/UnityProject/Assets/ThirdParty/CREDITS.md)

使用或再分发前必须检查对应资源的许可证范围。
