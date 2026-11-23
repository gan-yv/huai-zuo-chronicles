# Architecture Guide

## 分层

本项目的运行时代码按职责分为四层，依赖方向只能从表现层指向领域层：

1. `Core`：会话流程、存档、输入状态、音频和性能诊断。
2. `WorldState`：世界进度、区域、出生点、锚点、流式加载和街区事件。
3. `Player` / `Combat` / `Narrative` / `Interaction`：角色、战斗、叙事和交互领域。
4. `UI`：只读取领域状态和事件，不拥有玩法判定。

`WorldRuntime` 是跨场景世界会话入口，但具体状态按集合分类：已解锁锚点、已完成流式区域、已清理遭遇、已开始事件和已完成事件。

## 共享服务

- `GameplayUiState`：统一判断弹窗、对话、地图、案卷、背包、暂停和结局是否阻止玩法输入。
- `CombatRegistry`：自动登记动态启用的战斗单位，是锁定、血条和技能查询的统一来源。
- `CombatTargeting`：统一最近敌人和正面目标筛选，避免各组件各自实现。
- `CombatFeedback`：统一上报伤害位置、数值、印反应和玩家受击。
- `WorldStreamingDirector`：统一管理预载优先级、加载间隔和并发预算。
- `WorldContentTracker`：统一把街区事件状态提供给案卷、地图和 HUD。

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

新增街区事件：

1. 使用 `WorldContentEvent` 保存事件 ID、所需遭遇和已有物品奖励。
2. 使用 `WorldStoryNpcInteractable` 驱动阶段对话。
3. 在 `WorldContentTracker` 注册标题、地点、地图坐标和提示。

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
