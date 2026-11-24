# Demo02

当前开发中的 3D 城市开放区域冒险游戏。本轮只制作广陵“旧街回潮”垂直切片，目标时长 15～20 分钟。

## 必读入口

- [设定总纲](docs/Demo02_Design_Bible.md)：世界规则、历史、城市、主线、能力与主角。
- [制作文档](docs/Demo02_Production.md)：广陵 Demo 规格、制作顺序和 Unity 内容组织。
- [广陵美术方向规范](docs/Guangling_Art_Bible.md)：色彩、建筑尺度、材质、角色、灯光和性能预算。
- [美术替换指南](docs/Art_Replacement_Guide.md)：公开素材来源、授权、Unity 导入和灰盒替换流程。
- [公开素材管线](docs/Public_Asset_Pipeline.md)：授权规则、自动下载、校验和署名流程。
- [开放世界框架](docs/OpenWorld_Framework.md)：队伍、探索体力、区域锚点与长期内容结构。
- [代码架构](docs/Architecture.md)：模块分层、共享服务、扩展规则和依赖边界。
- [工程检查清单](docs/Engineering_Checklist.md)：稳定基线、发布和美术替换流程。
- [Git 与工程规范](../CONTRIBUTING.md)：分支、提交、拉取请求、LFS 和提交前检查。

## 当前状态

- 世界设定和广陵 Demo 制作范围已整理。
- Unity 6000.5.7f1 URP 工程和广陵灰盒已经建立。
- 第三方美术样板、PBR 材质生成、独立预览场景和授权校验流程已经可用。
- 官署前街 48 米美术试点、灰瓦坡顶、木构立面和完整主镜头已经生成。
- 主角、途印伙伴和官差的角色比例与剪影代理已经进入美术试点街。
- Kenney Animated Characters Protagonists 的风格化现成角色已接入可玩灰盒和美术试点。
- 现成木构和坡顶建筑作为测试样板保留，默认场景暂时使用原灰盒以保证性能。
- 首批 CC0 中式屏风、扶手椅、茶桌和柜子已下载并接入茶肆与旧仓。
- 公开素材通过脚本下载，作者、来源、许可证和哈希进入自动校验。
- 新增 OpenGameArt CC-BY 3.0 探索音乐，并在游戏与作品集署名中保留作者信息。
- 战斗加入蓄力预警、受击硬直和击退；威胁提示只显示附近敌人。
- 地图和 HUD 完成首轮降噪，时间层切换增加环境色温和过渡反馈。
- 四席队伍框架、两名角色档案、冲刺体力与空中滑翔已经接入可玩场景。
- 角色核心技能、终结技能量、双印错位反应和可解锁地区锚点已接入。
- 统一战斗注册表和区域遭遇导演已经拆分，封锁可调度增援巡逻。
- 首批增援遭遇已迁移到异步附加场景，复用现有角色资源完成加载与卸载。
- 流式巡哨的清理状态已写入存档，东关街和彩衣街拥有独立内容层。
- 流式区域加入集中预载队列，并按紧急程度和距离限制同时加载数量。
- 启动窗口按当前显示器可用空间自动选择最大 16:9 分辨率。
- 东关街和彩衣街加入“交谈、清剿、交付、领奖”的流式街区事件闭环。
- 东关街加入利用风场与滑翔系统的五检查点屋顶航线，完成后记录世界进度。
- 东关街说书人新增“纸鸢旧信”短支线，把时间层调查、屋顶风廊、双角色协作和街区选择串成完整流程。
- 非战斗伙伴“阿鱼”会在广陵街区同行，可随队伍换人切换跟随目标并与其交谈。
- 队伍 HUD 增加独立伙伴卡，明确阿鱼不占战斗席位。
- 对话和界面接管时会立即复位交互高亮，不再留下偏转物体。

![东关风廊屋顶航线](docs/screenshots/openworld-wind-course.png)

![东关纸鸢旧信短支线](docs/screenshots/openworld-paper-kite.png)

![纸鸢旧信最终选择](docs/screenshots/openworld-paper-kite-choice.png)

![上班鱼角色美术试点](docs/screenshots/anime-fish-pilot.png)

![上班鱼标题角色展示](docs/screenshots/title-anime-fish.png)

![阿鱼非战斗同行伙伴](docs/screenshots/work-fish-companion.png)

![东关街流式街区事件](docs/screenshots/openworld-street-story.png)

![街区事件地图标记](docs/screenshots/openworld-content-map.png)

![案卷中的街区事件进度](docs/screenshots/openworld-content-journal.png)

![彩衣街精英战](docs/screenshots/openworld-elite-encounter.png)

![CC0 中式茶席素材接入](docs/screenshots/teahouse-public-assets.png)

![四席队伍与滑翔探索框架](docs/screenshots/openworld-glide.png)

![技能、终结技与地区锚点框架](docs/screenshots/openworld-abilities-anchors.png)

![区域封锁与增援调度](docs/screenshots/openworld-encounter-support.png)

![异步加载的增援区域](docs/screenshots/openworld-streaming-support.png)

![流式预载与进入状态](docs/screenshots/openworld-streaming-budget.png)

![自适应屏幕窗口](docs/screenshots/openworld-adaptive-window.png)

![原生 1920×1080 无边框全屏](docs/screenshots/openworld-native-fullscreen.png)

## 工程目录

```text
demo02/
├─ README.md
├─ UnityProject/              # Unity 运行工程
│  ├─ Assets/
│  │  ├─ Demo02/             # 本项目运行资源
│  │  │  ├─ Art/
│  │  │  ├─ Audio/
│  │  │  ├─ Data/
│  │  │  ├─ Prefabs/
│  │  │  ├─ Scenes/
│  │  │  ├─ Scripts/
│  │  │  ├─ Settings/
│  │  │  ├─ Tests/
│  │  │  └─ UI/
│  │  └─ ThirdParty/         # 经许可使用的第三方资源
│  ├─ Packages/              # Unity 包清单
│  └─ ProjectSettings/       # Unity 项目设置
├─ SourceAssets/              # 尚未进入 Unity 的源素材
│  ├─ AnimationSource/
│  ├─ ArtSource/
│  ├─ AudioSource/
│  └─ References/
├─ Builds/                    # 本地构建产物，不提交
├─ Playtests/                 # 试玩记录和模板
├─ tools/                     # 构建与校验脚本
├─ docs/
│  ├─ Demo02_Design_Bible.md
│  ├─ Demo02_Production.md
│  └─ archive/        # 已废弃的早期草案
└─ README.md
```

旧方向的 2D 原型位于 [`../demo01/`](../demo01/README.md)，不会迁入 Demo02。
