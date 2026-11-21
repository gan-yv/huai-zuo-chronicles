# Demo02 运行资源

本目录只存放 Unity 实际加载和打包的资源。原始 PSD、Blend、音频工程和参考资料放在 `demo02/SourceAssets/`，导出后再放入这里。

## 目录职责

| 目录 | 内容 |
|---|---|
| `Art/Characters/` | 角色模型、贴图、材质和动画相关资源 |
| `Art/Environment/` | 城市建筑、街道、地形和环境贴图 |
| `Art/Materials/` | 共用材质与材质变体 |
| `Art/Props/` | 可交互物件和场景摆件 |
| `Art/UI/` | 界面贴图、图标和字体资源 |
| `Audio/Music/` | 正式背景音乐 |
| `Audio/SFX/` | 战斗、交互和环境音效 |
| `Audio/Voice/` | 配音资源 |
| `Data/Characters/` | 角色数据 |
| `Data/Dialogue/` | 对话、旁白和本地化文本源 |
| `Data/Encounters/` | 战斗和敌群配置 |
| `Data/Items/` | 物品、线索和账册等数据 |
| `Data/Quests/` | 任务、阶段和目标数据 |
| `Data/WorldState/` | 街区状态、选择结果和持久变化 |
| `Prefabs/Characters/` | 角色预制体 |
| `Prefabs/Environment/` | 建筑、街道和场景预制体 |
| `Prefabs/Gameplay/` | 交互点、敌人和玩法装置 |
| `Prefabs/UI/` | 界面预制体 |
| `Scenes/Boot/` | 启动、加载和全局场景 |
| `Scenes/Guangling/` | 广陵 Demo 场景 |
| `Scenes/Tests/` | 仅供开发测试的场景 |
| `Scripts/Core/` | 启动、服务、存档和全局基础设施 |
| `Scripts/Player/` | 移动、镜头和输入 |
| `Scripts/Combat/` | 战斗、伤害、敌人和残世印 |
| `Scripts/Interaction/` | 调查、拾取、对话入口和修复工具 |
| `Scripts/Narrative/` | 任务、对白和剧情流程 |
| `Scripts/WorldState/` | 时层、街区状态和选择结果 |
| `Scripts/UI/` | 运行时界面逻辑 |
| `Scripts/Editor/` | 仅编辑器使用的工具，必须位于 `Editor` 目录 |
| `Settings/` | 输入、渲染和项目级配置 |
| `Tests/EditMode/` | 不启动游戏的单元测试 |
| `Tests/PlayMode/` | 在 Unity 运行环境中执行的测试 |
| `UI/` | UI Toolkit、主题和样式资源 |

## 规则

- 路径使用英文和数字，不使用空格。
- 每个 `.meta` 文件必须与对应资源一起提交。
- 不把临时测试资源放进正式场景。
- 不从 `SourceAssets/` 直接引用文件。
- 第三方资源统一放在 `Assets/ThirdParty/` 并记录许可证。
