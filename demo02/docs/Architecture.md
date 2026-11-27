# Architecture Guide

## 分层

本项目的运行时代码按职责分为四层，依赖方向只能从表现层指向领域层：

1. `Core`：会话流程、存档、输入状态、音频和性能诊断。
2. `WorldState`：世界进度、区域、出生点、锚点、流式加载和街区事件。
3. `Player` / `Combat` / `Narrative` / `Interaction`：角色、战斗、叙事和交互领域。
4. `UI`：只读取领域状态和事件，不拥有玩法判定。

## 程序集边界

当前工程使用三个显式程序集：

| 程序集 | 范围 | 依赖 |
|---|---|---|
| `Demo02.Runtime` | `Scripts` 下的运行时领域与表现代码 | Input System、UGUI |
| `Demo02.Editor` | 内容生成、场景装配、构建和验证工具 | `Demo02.Runtime`、Unity 编辑器与 URP |
| `Demo02.Tests.EditMode` | EditMode 测试入口 | `Demo02.Runtime`、`Demo02.Editor`、Test Framework |

运行时代码不得引用 `Demo02.Editor`，编辑器不得依赖测试程序集。架构测试
`Demo02ArchitectureAudit` 会从 `.asmdef` 和运行时源码两侧检查这些约束。
运行时代码也不得直接读取 `Environment.GetCommandLineArgs()`；所有预览、截图、
性能采样和测试启动参数统一通过 `RuntimeLaunchOptions` 获取。下一阶段会把
参数判断从各控制器进一步移动到独立的预览场景运行器。

`GameServices` 是运行时 Composition Root 的入口。服务通过明确接口注册和解析，
首个迁移契约 `IGameFlowState` 已由 `GameFlowController` 注册，UI 和快捷存档
不再必须依赖具体控制器实例。旧静态单例在迁移期只作为兼容回退，新增代码应
优先通过 `GameServices` 获取服务。

`IContentProvider` 隔离统一内容目录的读取方式。默认实现
`ResourcesContentProvider` 负责 `Resources` 加载和目录缓存；
`GameContentRegistry` 保留为兼容门面。未来 Addressables 或其他内容包实现只需
注册新的 `IContentProvider`，玩法代码不需要修改。

下一阶段在该边界内继续拆分 `Core`、`Content`、`World`、`Gameplay`、
`Narrative` 和 `Presentation`，避免一次拆成过多小程序集。

`WorldRuntime` 是跨场景世界会话入口，但具体状态按集合分类：已解锁锚点、已完成流式区域、已清理遭遇、已开始事件和已完成事件。

## 共享服务

- `GameplayUiState`：统一判断弹窗、对话、地图、案卷、背包、暂停和结局是否阻止玩法输入。
- `CombatRegistry`：自动登记动态启用的战斗单位，是锁定、血条和技能查询的统一来源。
- `CombatTargeting`：统一最近敌人和正面目标筛选，避免各组件各自实现。
- `CombatFeedback`：统一上报伤害位置、数值、印反应和玩家受击。
- `WorldStreamingDirector`：统一管理预载优先级、加载间隔和并发预算。
- `WorldContentTracker`：统一把街区事件状态提供给案卷、地图和 HUD。
- `GameplayUiState`：维护统一模态栈，界面在打开和关闭时登记所有权；玩法输入、
  暂停流程和快捷键查询都通过该栈判断当前是否被界面阻塞。

新增模态界面时，必须在一处且仅一处在打开和关闭生命周期中调用
`GameplayUiState.RegisterModal/UnregisterModal`。不要继续在各控制器中手写
“检查其他所有界面类型”的条件，否则新增界面会遗漏关闭、场景切换和输入屏蔽。

## 扩展规则

新增角色：

1. 创建 `CharacterDefinition`。
2. 复用现有角色预制体并配置 `CharacterRuntime`。
3. 仅在需要新机制时扩展技能样式，不复制输入和 HUD 逻辑。

新增敌人：

1. 复用现有 `EnemyAI`、`Combatant`、`ResonanceStatus` 和 `EnemyBreakStatus`。
2. 通过 `WorldEncounterDefinition` 配置激活与增援关系。
3. 将区域内容放入独立附加场景，由流式导演统一加载。

精英敌人的视觉模型统一放在 `VisualReplacementSlot` 下。替换外观时保留敌人根节点及其战斗组件，只替换视觉槽内的预制体。

新增流式区域：

1. 在 `Assets/Demo02/Data/WorldStreaming` 创建
   `WorldStreamingRegionDefinition`。
2. 配置区域 ID、场景名、世界位置、加载/预加载/卸载半径、预加载策略和遭遇
   内容；流式根对象、遭遇对象与敌人名称也随定义保存。
3. 将定义接入 `WorldStreamingRegionSeeder` 的构建数据，执行内容管线后由
   构建器生成主场景触发器和对应附加场景。
4. `WorldStreamingBudgetAudit` 会检查单区域的 750 个对象、300 个渲染器、
   24 个材质、100 万顶点和 100 万三角形预算。

运行时的 `WorldStreamingDirector` 只解析一次玩家队伍，并为所有区域提供活动
角色。区域在附加场景加载完成后缓存本场景的 `WorldEncounterZone` 与
`WorldContentEvent`，卸载判定不会扫描其他区域。

新增街区事件：

1. 使用 `WorldContentEvent` 保存事件 ID、所需遭遇和已有物品奖励。
2. 使用 `WorldStoryNpcInteractable` 驱动阶段对话。
3. 在 `WorldContentTracker` 注册标题、地点、地图坐标和提示。

新增条件剧情 NPC：

1. 将 NPC 视觉与交互对象放在独立根节点上。
2. 使用 `WorldNpcAvailability` 保存最低章节阶段、必要结局、必要事实和禁止事实。
3. 对白继续使用 `DialogueScriptDefinition`，不要按显示状态复制交互逻辑。
4. 条件角色会在阶段、事实或读档状态变化后重新计算可见性。

新增章节：

1. 创建 `ChapterDefinition`，配置章节 ID、入口场景、出生点、初始区域和
   `CaseDefinition`。
2. 将章节加入 `GameContentCatalog`，并决定默认章节。
3. 启动器通过 `GameContentRegistry` 读取默认章节，不再在代码中写死场景名。
4. 存档使用 `ChapterId` 和 `CaseId` 恢复章节身份；入口场景与出生点仍由章节
   定义提供。
5. 执行 `Demo02/Content/Rebuild Game Content Catalog` 和全量校验。

内容目录位于：

```text
Assets/Demo02/Resources/Data/Demo02ContentCatalog.asset
Assets/Demo02/Data/Chapters/Chapter01.asset
```

`GameContentSeeder.ValidateForBatch` 会同时检查编辑器资产和 Resources 运行时
加载结果，避免出现“编辑器可读、构建后为空”的失效引用。

## 内容构建模块

`Demo02ContentPipeline` 统一管理内容生成和验证顺序。内置模块按职责拆分：

| 顺序 | 模块 | 职责 |
|---:|---|---|
| 100 | `definitions` | 角色、遭遇、流式区域、章节、案件与统一内容目录 |
| 200 | `characters` | 角色模型、材质、骨骼、动作和预制体 |
| 300 | `environment` | 城市环境、街景、音频与公共道具 |
| 350 | `asset-budget` | 环境贴图分辨率、压缩、流式 Mipmap 与导入校验 |
| 400 | `world-scenes` | 主场景、流式场景、启动场景与 Build Settings |
| 500 | `mesh-budget` | 网格可读副本和最终网格预算 |
| 600 | `data-audit` | 孤儿资产检测、目录 ID 校验和内容清单导出 |

资源预算必须排在环境生成之后，因为环境导入器会重新设置贴图参数。内容管线
按 `Order` 排序执行，并保留同一 ID 的最新注册模块。

新增系统可实现 `IDemo02ContentModule` 并注册：

```csharp
Demo02ContentPipeline.RegisterModule(new MyContentModule());
```

每个模块同时提供 `Build()` 和 `Validate()`，避免生成逻辑与校验规则分离到
不同工具中。菜单入口：

```text
Demo02/Content/Rebuild All Content Modules
Demo02/Content/Validate All Content Modules
```

## 主场景装配器

`Demo02GreyboxBuilder` 只保留编辑器菜单、批处理入口和兼容 API。
`Demo02GreyboxSceneAssembler` 负责装配顺序，具体内容按职责委派：

| 装配器 | 职责 |
|---|---|
| `Demo02GreyboxRegionAssembler` | 地形、街区结构、区域触发器与传送门 |
| `Demo02GreyboxNpcAssembler` | 角色视觉、NPC 放置与 NPC 专属组件 |
| `Demo02GreyboxTaskAssembler` | 线索、任务交互、奖励、结局与街区故事 |
| `Demo02GreyboxGameplayAssembler` | 玩家编队、敌人、遭遇、流式场景、时间和移动玩法 |
| `Demo02GreyboxUiAssembler` | 游戏会话、HUD、章节标记与场景表现 |

`Demo02SceneObjectFactory` 只提供跨装配器共享的基础对象原语。
`Demo02RegressionVerifier` 独立承载编辑器玩法回归，不再混入场景生成流程。

## 约束

- UI 不直接修改战斗数值或世界进度，只调用明确命令。
- 场景构建器负责生成和校准内容，可复用资源必须通过构建器生成。
- 新素材必须进入素材清单和许可证校验，不能把来源不明资源直接放入 Unity。
- 不在单个控制器中继续堆积新的全局状态；跨场景状态应进入 `WorldRuntime` 或独立服务。

## 分辨率

- 发布构建默认使用显示器原生分辨率的无边框全屏；设置中可切换窗口化。
- 窗口化模式按显示器可用空间计算最大 16:9 客户区。
- `-screen-width`、`-screen-height` 只用于开发期固定分辨率对比，不是默认运行方式。
- 性能报告记录实际采样分辨率，跨版本比较时应先确认分辨率一致。
