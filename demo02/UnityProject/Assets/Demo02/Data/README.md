# Demo02 数据资产

本目录保存运行时和编辑器共用的 ScriptableObject 内容。资源之间优先通过对象引用
连接；需要字符串查询时，先在统一目录中按稳定 ID 查找，不要直接拼资产路径。

## 运行时入口

```text
Assets/Demo02/Resources/Data/Demo02ContentCatalog.asset
```

`GameContentRegistry` 通过 `Data/Demo02ContentCatalog` 加载此资产。目录当前包含：

| 类型 | 数量 | 用途 |
|---|---:|---|
| `ChapterDefinition` | 1 | 章节入口场景、出生点、区域和案件资产 |
| `CharacterDefinition` | 2 | 角色能力、体力和成长数据 |
| `WorldEncounterDefinition` | 4 | 战斗激活半径、敌人配置与增援关系 |
| `WorldStreamingRegionDefinition` | 3 | 流式附加场景的位置、半径和内容 |

案件内的证据、证词、对白、故事事实、物品和可选目标由
`CaseDefinition` 统一持有，因此不另建碎片化文本资产。

`Dialogue/` 与 `Items/` 目前为预留目录，不得在单章节内重复拆分同类数据。只有
出现跨章节共享对白或独立物品库时，才应引入新的数据资产类型并加入统一目录。

`Dialogue/` 与 `Items/` 目前为预留目录，不得在单章节内重复拆分同类数据。只有
出现跨章节共享对白或独立物品库时，才应引入新的数据资产类型并加入统一目录。

## 目录结构

| 目录 | 资产类型 | 命名规则 |
|---|---|---|
| `Chapters/` | `ChapterDefinition` | `Chapter##.asset` |
| `Cases/` | `CaseDefinition` | `Chapter##_Case.asset` |
| `Characters/` | `CharacterDefinition` | `CHR_<Name>.asset` |
| `Encounters/` | `WorldEncounterDefinition` | `ENC_<Name>.asset` |
| `WorldStreaming/` | `WorldStreamingRegionDefinition` | `STREAM_<Name>.asset` |

运行时目录位于 `Assets/Demo02/Resources/Data/`，只放启动流程必须加载的目录资产，
不把普通内容资产移入 `Resources`。

每次完整内容重建还会生成：

```text
docs/generated/content-manifest.json
```

清单包含稳定 ID、资产路径、章节入口、案件路径和流式区域遭遇引用，便于代码审查
和版本差异比较。

## 数据契约

| 类型 | 稳定 ID | 关键引用 |
|---|---|---|
| 章节 | `ChapterId` | `SceneName`、出生点、区域、`CaseDefinition` |
| 案件 | `CaseId` | 证据、对白脚本、故事事实、物品、可选目标 |
| 角色 | `CharacterId` | 技能、耐力、能量与残世印 |
| 遭遇 | `EncounterId` | 敌人参数、默认状态、增援遭遇 ID |
| 流式区域 | `RegionId` | 场景名、世界坐标、半径、遭遇资产 |

稳定 ID 一旦进入存档或场景序列化，不随显示名称变化。删除或重命名 ID 前必须
提供存档迁移，并同步更新验证规则。

## 扩展流程

1. 在对应目录创建或由 Seeder 生成数据资产。
2. 为定义填写稳定 ID、显示名称和运行参数。
3. 加入 `GameContentCatalog`，由 `GameContentSeeder` 统一重建。
4. 在 `GameContentCatalog.Validate()` 中补充跨资产约束。
5. 运行内容验证和全量玩法验证。
6. 场景只引用资产或稳定 ID，不硬编码数据目录路径。

## 验证

菜单入口：

```text
Demo02/Content/Rebuild All Content Modules
Demo02/Content/Validate All Content Modules
```

批量入口：

```text
Demo02.Editor.Demo02ContentPipeline.BuildAll
Demo02.Editor.Demo02GreyboxBuilder.VerifyAllForBatch
```

验证覆盖空引用、重复 ID、场景是否存在、流式区域预算、Resources 运行时加载和
目录资产数量一致性。`data-audit` 模块还会拒绝未加入统一目录的
ScriptableObject 资产。

## 迁移

旧目录迁移由 Seeder 使用 `AssetDatabase.MoveAsset` 执行，保留 `.meta` GUID：

| 旧路径 | 新路径 |
|---|---|
| `Data/WorldState/STREAM_*` | `Data/WorldStreaming/STREAM_*` |
| `Data/Quests/Chapter01_Case.asset` | `Data/Cases/Chapter01_Case.asset` |
| `Resources/Demo02ContentCatalog.asset` | `Resources/Data/Demo02ContentCatalog.asset` |

迁移工具会拒绝覆盖新路径中的同名资产，避免无声丢失数据。
