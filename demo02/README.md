# 淮左纪事 / Demo02

《淮左纪事》是一款本地单机 3D 动作冒险垂直切片。玩家在广陵旧街调查一起
“名字被从册页抹去”的盐引案，通过战斗、时空调查、人物证词和最终选择决定
盐引巷是否重新回到城市记录。

当前版本：**1.75.2**

引擎：**Unity 6000.5.7f1 / URP**

目标平台：**Windows x64**

![荧在广陵正式场景中的主角视角](docs/screenshots/lumine-gameplay.png)

## 快速开始

### 直接运行

本地 Windows 构建：

```text
demo02/Builds/WindowsDemo/HuaiZuoChronicles.exe
```

### 使用 Unity

1. 使用 Unity Hub 打开 `demo02/UnityProject`。
2. 使用 Unity `6000.5.7f1`，等待首次导入和脚本编译完成。
3. 打开 `Assets/Demo02/Scenes/Boot/Boot.unity`。
4. 点击 Play。
5. 从标题界面开始游戏。

不要在资源未完成导入时运行内容重建；首次打开项目应先等待 Unity 完成资源导入。

## 操作

| 按键 | 功能 |
|---|---|
| `WASD` | 移动 |
| `Shift` | 冲刺，消耗体力 |
| `Space` | 跳跃；空中再次按下展开滑翔 |
| 鼠标右键按住 | 旋转第三人称镜头 |
| 鼠标左键 | 三段普通攻击 |
| `Ctrl` | 闪避；成功躲开橙色预警可触发见隙反击 |
| `F` | 当前角色核心技能 |
| `R` | 能量满时释放终结技 |
| `H` | 快捷使用第一种可用恢复物品 |
| `1` / `2` | 直接切换已解锁角色 |
| `Tab` | 循环切换队伍成员 |
| `E` | 调查、交谈、拾取、锚点与修复交互 |
| `Q` | 切换现世 / 旧时层 |
| `J` | 调查案卷 |
| `B` | 背包 |
| `T` | 证词对照 |
| `M` | 地图与快速旅行 |
| `F1` | 操作帮助 |
| `F5` / `F9` | 快速保存 / 读取当前手动槽 |
| `Esc` | 暂停或关闭当前界面 |

## 当前可玩范围

| 模块 | 当前内容 |
|---|---|
| 章节 | 第一章“消失的盐引” |
| 地区 | 官署前街、盐仓横街、回潮巷、旧仓、东关街、彩衣街、林间小屋 |
| 可玩角色 | 荧与途印向导，两席实际可玩、四席队伍框架 |
| 伙伴 | 非战斗同行伙伴阿鱼 |
| 战斗 | 三段连式、技能、终结技、闪避、见隙反击、敌人循环重击、韧性与破防 |
| 调查 | 三处主线线索、时间锚点、证词矛盾、可选支线 |
| 剧情人物 | 沈伯、沈砚、严槐、搬运工、阿禾、陆崇及街坊 NPC |
| 流式场景 | 盐仓增援、东关巡哨、彩衣望风 |
| 结局 | 保住盐引巷 / 封存盐引巷，并支持继续探索街区 |
| 存档 | 自动存档、三个手动槽、章节与案件 ID、故事事实和世界状态 |

## 仓库结构

```text
demo02/
├─ README.md
├─ CHANGELOG.md                  # 版本级变更记录
├─ UnityProject/
│  ├─ Assets/
│  │  ├─ Demo02/
│  │  │  ├─ Art/                 # 角色、环境、道具和 UI 资源
│  │  │  ├─ Audio/               # 音乐、音效与配音
│  │  │  ├─ Data/                # ScriptableObject 内容资产
│  │  │  ├─ Prefabs/             # 运行时预制体
│  │  │  ├─ Resources/Data/      # 唯一运行时内容目录
│  │  │  ├─ Scenes/              # Boot、广陵主场景与流式场景
│  │  │  ├─ Scripts/             # 运行时和编辑器代码
│  │  │  ├─ Settings/            # 输入与渲染配置
│  │  │  ├─ Tests/               # EditMode / PlayMode 测试
│  │  │  └─ UI/                  # UI 主题和样式资源
│  │  └─ ThirdParty/             # 有授权记录的第三方资源
│  ├─ Packages/                  # Unity 包清单
│  └─ ProjectSettings/           # Unity 项目设置
├─ Builds/                       # 本地构建产物
├─ Playtests/                    # 构建、验证和性能日志
├─ SourceAssets/                 # 未导入 Unity 的源素材
├─ docs/                         # 设计、架构、资产和性能文档
└─ tools/                        # 自动化脚本
```

## 代码分层

| 层 | 目录 | 职责 |
|---|---|---|
| 基础设施 | `Scripts/Core` | 启动、存档、设置、音频、诊断 |
| 玩家 | `Scripts/Player` | 移动、镜头、队伍、角色运行时、快捷物品 |
| 战斗 | `Scripts/Combat` | 伤害、能力、敌人、韧性、连式与反馈 |
| 交互 | `Scripts/Interaction` | 调查、NPC、拾取、锚点、时间缝 |
| 叙事 | `Scripts/Narrative` | 章节、案件、对白、事实和目标条件 |
| 世界状态 | `Scripts/WorldState` | 区域、流式、遭遇、锚点和条件 NPC |
| 界面 | `Scripts/UI` | HUD、案卷、地图、背包、设置和结局 |
| 编辑器 | `Scripts/Editor` | Seeder、内容管线、构建与自动验证 |

依赖方向应从 UI 和表现层指向领域数据，不应让数据资产反向依赖 UI 状态。

## 数据模型

运行时唯一入口：

```text
Assets/Demo02/Resources/Data/Demo02ContentCatalog.asset
```

`GameContentCatalog` 统一注册：

- `ChapterDefinition`：章节入口、出生点、区域和案件。
- `CharacterDefinition`：角色体力、能量、技能和残世印。
- `WorldEncounterDefinition`：遭遇半径、敌人参数和增援关系。
- `WorldStreamingRegionDefinition`：附加场景、位置、半径和内容。

案件内的证据、证词、对白、事实、物品和可选目标由 `CaseDefinition` 持有，
避免产生大量碎片化文本资产。

完整目录、架构和迁移规则见：

- [`UnityProject/Assets/Demo02/Data/README.md`](UnityProject/Assets/Demo02/Data/README.md)
- [内容管线](docs/Content_Pipeline.md)
- [代码架构](docs/Architecture.md)

### 稳定 ID

以下 ID 会进入场景、存档或跨资产引用：

- `ChapterId`
- `CaseId`
- `CharacterId`
- `EncounterId`
- `RegionId`
- 证据、事实、物品、对话和交互 ID

显示名称可以修改，稳定 ID 不应在没有存档迁移的情况下修改。

## 内容管线

统一入口：

```text
Demo02/Content/Rebuild All Content Modules
Demo02/Content/Validate All Content Modules
```

内置模块：

| 顺序 | 模块 | 职责 |
|---:|---|---|
| 100 | `definitions` | 角色、遭遇、流式区域、章节与案件 |
| 200 | `characters` | 模型、材质、骨骼、动画和角色预制体 |
| 300 | `environment` | 城市环境、街景、音频和公共道具 |
| 350 | `asset-budget` | 纹理分辨率、压缩和导入预算 |
| 400 | `world-scenes` | 主场景、流式场景、Boot 与 Build Settings |
| 500 | `mesh-budget` | 网格可读副本与最终网格预算 |
| 600 | `data-audit` | 孤儿资产检测、目录校验和内容清单 |

批量重建：

```powershell
& 'D:\Unity2\set\6000.5.7f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'D:\GPTCODEX\CODEX\yangzhougame\demo02\UnityProject' `
  -executeMethod Demo02.Editor.Demo02ContentPipeline.BuildAll `
  -quit `
  -logFile 'D:\GPTCODEX\CODEX\yangzhougame\demo02\Playtests\build.log'
```

## 构建

菜单入口：

```text
Demo02/Build Windows Demo
```

批量构建：

```powershell
& 'D:\Unity2\set\6000.5.7f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'D:\GPTCODEX\CODEX\yangzhougame\demo02\UnityProject' `
  -executeMethod Demo02.Editor.Demo02Build.BuildWindowsDemo `
  -quit `
  -logFile 'D:\GPTCODEX\CODEX\yangzhougame\demo02\Playtests\build.log'
```

构建输出：

```text
demo02/Builds/WindowsDemo/
├─ HuaiZuoChronicles.exe
└─ BuildInfo.txt
```

## 验证

全量验证：

```powershell
& 'D:\Unity2\set\6000.5.7f1\Editor\Unity.exe' `
  -batchmode `
  -projectPath 'D:\GPTCODEX\CODEX\yangzhougame\demo02\UnityProject' `
  -executeMethod Demo02.Editor.Demo02GreyboxBuilder.VerifyAllForBatch `
  -quit `
  -logFile 'D:\GPTCODEX\CODEX\yangzhougame\demo02\Playtests\verify.log'
```

验证范围：

- 内容目录、Resources 加载和跨资产引用。
- 章节阶段、对白条件、选择和故事事实。
- 背包、快捷恢复、存档槽和状态恢复。
- 三段连式、完美闪避、技能和战斗伤害。
- 主场景结构、时间锚点、NPC 和流式场景。
- 区域预算、网格预算、材质与纹理导入设置。
- 未注册数据资产、稳定 ID 解析和内容清单生成。

内容清单：

```text
docs/generated/content-manifest.json
```

## 性能

最近一轮 30 秒 Windows 基准：

| 指标 | 数值 |
|---|---:|
| Windows 包体 | 约 728.6 MB |
| 平均 FPS | 约 210.5 |
| 1% Low FPS | 约 131.8 |
| 原生总分配 | 约 242.4 MB |
| 原生总保留 | 约 360.7 MB |

详细方法和历史数据见 [`docs/Performance_Budget.md`](docs/Performance_Budget.md)。

## 剧情扩展

- [`docs/Story_Upgrade.md`](docs/Story_Upgrade.md)：第一章主题、人物支线和内容版本。
- [`docs/OpenWorld_Framework.md`](docs/OpenWorld_Framework.md)：队伍、探索、遭遇和流式结构。
- [`docs/Character_Animation_Pipeline.md`](docs/Character_Animation_Pipeline.md)：荧与阿鱼的动作管线。

当前内容版本为 **第 16 版**，加入严槐家庭支线、陆崇当面对质、街坊结局反应和
结局后自由探索。

## 常见问题

### 场景启动后没有变化

确认运行的是最新构建，并完全退出旧进程。检查
`Builds/WindowsDemo/BuildInfo.txt` 中的版本和构建时间。

### 内容目录为空或引用丢失

运行：

```text
Demo02/Content/Rebuild All Content Modules
```

随后运行 `Validate All Content Modules`。不要手工复制 `.asset` 文件，以免破坏
GUID 引用。

### 新增资产后场景看不到

确认资产已加入 `GameContentCatalog`，再重建 `world-scenes` 模块和 Build
Settings。只在磁盘上新增文件不会自动加入运行时目录。

### 存档读取后角色或剧情状态不正确

检查稳定 ID 是否被修改，运行全量验证，并确认存档中的 `ChapterId`、`CaseId`
与故事事实仍存在于当前内容版本。

## 授权

- 第三方资源必须记录作者、来源、许可证和哈希。
- `Assets/ThirdParty/CREDITS.md` 与各素材目录的 `SOURCE.md` 是发布前必查项。
- 荧的角色素材存在商业使用和再配布限制，发布前必须重新确认授权。
- 不把来源不明或仅允许个人使用的资源提交到公开构建。

## 文档

- [设定总纲](docs/Demo02_Design_Bible.md)
- [制作文档](docs/Demo02_Production.md)
- [代码架构](docs/Architecture.md)
- [数据资产说明](UnityProject/Assets/Demo02/Data/README.md)
- [内容管线](docs/Content_Pipeline.md)
- [广陵美术方向](docs/Guangling_Art_Bible.md)
- [美术替换指南](docs/Art_Replacement_Guide.md)
- [公开素材管线](docs/Public_Asset_Pipeline.md)
- [性能预算](docs/Performance_Budget.md)
- [自动生成内容清单](docs/generated/content-manifest.json)
- [工程检查清单](docs/Engineering_Checklist.md)
- [Git 与工程规范](../CONTRIBUTING.md)

旧 2D 原型位于 [`../demo01/`](../demo01/README.md)，不迁入 Demo02。
