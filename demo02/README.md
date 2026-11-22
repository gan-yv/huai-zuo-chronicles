# Demo02

当前开发中的 3D 城市开放区域冒险游戏。本轮只制作广陵“旧街回潮”垂直切片，目标时长 15～20 分钟。

## 必读入口

- [设定总纲](docs/Demo02_Design_Bible.md)：世界规则、历史、城市、主线、能力与主角。
- [制作文档](docs/Demo02_Production.md)：广陵 Demo 规格、制作顺序和 Unity 内容组织。
- [广陵美术方向规范](docs/Guangling_Art_Bible.md)：色彩、建筑尺度、材质、角色、灯光和性能预算。
- [美术替换指南](docs/Art_Replacement_Guide.md)：公开素材来源、授权、Unity 导入和灰盒替换流程。
- [Git 与工程规范](../CONTRIBUTING.md)：分支、提交、拉取请求、LFS 和提交前检查。

## 当前状态

- 世界设定和广陵 Demo 制作范围已整理。
- Unity 6000.5.7f1 URP 工程和广陵灰盒已经建立。
- 第三方美术样板、PBR 材质生成、独立预览场景和授权校验流程已经可用。
- 官署前街 48 米美术试点、灰瓦坡顶、木构立面和完整主镜头已经生成。

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
