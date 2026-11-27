# AI 多样化与 UI 迁移

## 敌人行为档案

`EnemyAI` 通过 `EnemyArchetype` 选择三种基础行为：

| 档案 | 行为 |
|---|---|
| `Chaser` | 接近玩家并执行近战轻重攻击 |
| `Shield` | 慢速压近，正面受到玩家攻击时大幅减伤 |
| `Ranged` | 保持射击距离，过近时后撤，并发射可被闪避的投射物 |

遭遇定义包含主要敌人的行为档案。官署前街的首场遭遇使用追击、盾兵和远程兵
混编；数据驱动流式遭遇分别使用盾兵或远程兵档案。

彩衣街望风敌人是多阶段敌人：

1. 第一阶段追击并近战。
2. 第二阶段切换盾兵档案，并周期性冲锋。
3. 第三阶段切换远程档案，进行高频率远距压制。

伤害结算必须先调用 `EnemyAI.ModifyIncomingDamage`，否则盾兵正面减伤会被绕过。

## UI 迁移

新界面默认使用 UGUI 或 UI Toolkit。迁移顺序优先选择高频、持续显示的 HUD，
再处理低频弹窗：

1. `AbilityHud` 已迁移为一次性创建的 UGUI 层级，不再包含 `OnGUI`。
2. `PartyHud`、`EliteEnemyHud` 和 `ContextHintController` 已迁移为持久 UGUI
   层级，复用 `HudUiFactory` 创建 Canvas、面板、图片与文本。
3. `CombatHud`、`ObjectiveDisplay` 和 `QuestNavigationHud` 已迁移；敌人血条、
   破防/预警、目标标记、时间层过渡和帮助面板都由复用 UGUI 对象更新。
4. `CombatFeedbackHud`、`StoryFactNotification`、`WorldContentToast` 和
   `AreaBanner` 已迁移；浮动伤害、受击遮罩、支线旁证、街区完成提示和区域
   横幅都由持久 UGUI 与 CanvasGroup 控制。
5. 设置、存档和地图界面已迁移到 UGUI；键盘/手柄方向键、确认、取消、重绑定、
   存档覆盖确认、快速旅行和事件地图标记全部保留。
6. 对话界面已迁移到 UGUI，保留逐行推进、动态键位提示、四选项鼠标/键盘/手柄
   操作和对话镜头。
7. 下一次优先迁移剩余模态界面：案卷、证词和背包。
8. 模态界面迁移时必须沿用 `Demo02Input` 的导航、提交和取消动作。

HUD 每帧只更新已有 `RectTransform`、`Image` 和 `Text` 的值，不重新创建视觉树。
文本格式化限制在低频刷新中执行，避免持续产生字符串和布局分配。

统一阻塞判断来自 `GameplayUiState`。HUD 不应再逐项检查对话、地图、背包等具体
界面类型；新增模态界面会自动屏蔽 HUD 和玩法输入。
