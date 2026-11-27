# 自动化测试结构

## EditMode 测试

Unity Test Framework 用例位于：

```text
Assets/Demo02/Tests/Editor
```

测试按职责分组，并统一带有 `Demo02` 分类：

| 分类 | 覆盖范围 |
|---|---|
| `Save` | 版本迁移、校验和、损坏恢复、槽位读写 |
| `Story` | 对话脚本、时间层、章节阶段和结局 |
| `Combat` | 伤害、连招、闪避、敌人档案、背包和角色属性 |
| `Data` | 内容模块、统一目录、资源预算和跨资产引用 |
| `World` | 开放世界、流式场景和地图可读性 |
| `InputUi` | Input Actions、重绑定和 UGUI 迁移样板 |

运行全部分组：

```powershell
Unity.exe -batchmode -nographics `
  -projectPath <UnityProject> `
  -runTests -testPlatform EditMode `
  -testCategory Demo02 `
  -testResults <results.xml>
```

只运行一组时，将 `-testCategory Demo02` 换成对应分类，例如
`-testCategory Save`。

## 兼容批处理入口

`Demo02GreyboxBuilder.VerifyAllForBatch` 仍作为完整兼容入口，但只按测试组调度：

```text
VerifySaveSuiteForBatch
VerifyStorySuiteForBatch
VerifyCombatSuiteForBatch
VerifyDataSuiteForBatch
VerifyWorldSuiteForBatch
VerifyInputUiSuiteForBatch
```

每个入口也可以直接用于 `-executeMethod`，便于失败后只重跑对应领域。

## 代码分层

`Demo02RegressionVerifier` 只保留稳定门面和套件调度，不再包含具体测试实现。
各领域实现位于独立文件：

| 套件 | 职责 |
|---|---|
| `Demo02SaveRegressionSuite` | 存档格式、迁移和恢复 |
| `Demo02StoryRegressionSuite` | 对话、时间层和章节流程 |
| `Demo02CombatRegressionSuite` | 战斗、技能、敌人和背包 |
| `Demo02DataRegressionSuite` | 内容模块与目录验证 |
| `Demo02WorldRegressionSuite` | 开放世界、流式场景和地图 |
| `Demo02InputUiRegressionSuite` | 输入映射和 UI 架构 |

EditMode 测试直接调用对应领域套件，`Demo02GreyboxBuilder` 和
`Demo02RegressionVerifier` 仅用于兼容既有命令。

## PlayMode 测试

运行时代码另由 `Demo02.Tests.PlayMode` 程序集覆盖。当前冒烟测试验证：

- `Boot` 场景可加载，并可从统一内容目录解析默认章节和案件。
- 默认章节正式场景可加载，核心世界、流程、存档和队伍服务能够启动。
- 目录中的每个流式场景都存在于 Build Settings，并可完成附加加载和卸载。

运行 PlayMode 分组：

```powershell
Unity.exe -batchmode -nographics `
  -projectPath <UnityProject> `
  -runTests -testPlatform PlayMode `
  -testCategory PlayMode `
  -testResults <results.xml>
```

PlayMode 入口使用真实场景加载，不替代 EditMode 的静态内容和存档校验；两类
测试必须同时通过才能发布。
