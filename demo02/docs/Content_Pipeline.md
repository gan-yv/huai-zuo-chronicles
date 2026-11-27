# 内容构建与扩展管线

## 启动流程

建议把内容相关工作绑定到 `InitializeOnLoadMethod` 或由章节包初始化器注册，
不要直接修改 `Demo02GreyboxBuilder`：

```csharp
public sealed class RaidContentModule : IDemo02ContentModule
{
    public string Id => "raid-content";
    public int Order => 350;

    public void Build()
    {
        // 生成预制体、数据资产、场景或 Build Settings。
    }

    public void Validate()
    {
        // 校验引用、入口、资源和可运行时加载状态。
    }
}
```

注册只保留同一 ID 的最新模块，因此域重载后可以安全重复注册。

## 章节与模块的关系

- `ChapterDefinition` 负责运行时入口：场景、出生点、区域和案件资产。
- `IDemo02ContentModule` 负责编辑器生成：模型、数据、场景和资源校验。
- `Demo02Build` 先执行内容模块校验，再执行完整内容生成和 Windows 打包。
- `Demo02GreyboxBuilder.VerifyAllForBatch` 先运行内容模块验证，再执行玩法回归。

新增章节通常需要同时添加一个 `ChapterDefinition` 和一个生成该章节内容的
内容模块，但不需要修改启动器和主构建调用列表。

## 统一内容目录

`definitions` 模块按以下顺序生成基础数据：

1. 角色定义。
2. 遭遇定义。
3. 流式区域定义。
4. 章节、案件和统一目录。

统一目录位于：

```text
Assets/Demo02/Resources/Data/Demo02ContentCatalog.asset
```

`GameContentRegistry` 通过 `Data/Demo02ContentCatalog` 加载目录，并按稳定 ID
查询章节、角色、遭遇和流式区域。验证会检查重复 ID、空引用、场景是否存在以及
流式区域引用的遭遇是否也在目录中。

新增核心数据类型的标准流程：

1. 创建 ScriptableObject 定义类型。
2. 在选择器 Seeder 中生成或更新资产。
3. 将列表加入 `GameContentCatalog`。
4. 在 `GameContentCatalog.Validate()` 增加跨资产约束。
5. 用 `AssetDatabase.MoveAsset` 迁移旧路径，保留 `.meta` GUID。
6. 运行完整内容验证和全量玩法回归。

详细字段与命名规则见：

```text
Assets/Demo02/Data/README.md
```

## 流式区域

`definitions` 模块生成 `WorldStreamingRegionDefinition`，`world-scenes` 模块读取
这些资产并生成主场景触发器、附加场景与 Build Settings。区域资产位于：

```text
Assets/Demo02/Data/WorldStreaming/STREAM_*.asset
```

`world-scenes` 验证会检查区域 ID、场景、半径和遭遇引用，并由
`WorldStreamingBudgetAudit` 输出对象、渲染器、材质、顶点和三角形预算。新增
街区内容不需要再复制 `Demo02GreyboxBuilder` 中的三组硬编码区域参数。
