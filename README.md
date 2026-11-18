# 春江花月夜：异本

一款以架空江海城市群为舞台的 2D 国风叙事悬疑游戏。

现代文献修复师进入一部残缺的《春江花月夜》异本，发现扬州、海陵、通州三地各保存着不同的诗句版本。有人改动了其中一处文字，而这次改动可能在春夜灯会上被解释为开战信号。

玩家通过调查人物、比对异文、选择传播哪一种版本，决定一座城市如何讲述自己。

## 当前状态

- 阶段：Demo v0.1 可玩灰盒
- 引擎：Unity 6000.5.7f1
- 平台：PC，横屏 1920×1080
- 核心体验：叙事调查、异文比对、有限选择、国风悬疑
- 首卷目标：25～30 分钟的“扬州本”
- 制作方式：个人开发、AI 辅助文案与美术、人工完成取舍和最终审核

## 当前可玩内容

- 标题与“开始校对”入口。
- 修复室残页与墨痕点击流程。
- 书坊开场对白。
- 陆掌柜、顾师父互斥调查。
- “待/照”双页异文比对、上版与交印确认。
- 两条河岸结果、残缺校勘笺、结束页和完整重开。

打开 `UnityProject`，进入 `Assets/Scenes/Demo.unity` 后点击播放即可测试。

Windows 64 位构建可从 Unity 菜单
`Tools > 春江花月夜 > Build Windows Demo` 生成。

## 当前文档

- [《春江花月夜：异本》主设计大纲](docs/Spring_River_Moonlight_Yiben_GDD.md)
- [完整世界观与主线闭环](docs/World_and_Story_Arc.md)
- [整体游戏逻辑与状态系统](docs/Game_Logic_System_Design.md)
- [扬州卷 Demo v0.1 实现规格](docs/Demo_v0.1_Implementation_Spec.md)
- [扬州卷 Demo 开场脚本](docs/Demo_v0.1_Opening_Script.md)
- [Git 与 GitHub 协作流程](docs/Development_Workflow.md)
- [制作路线与里程碑](docs/Production_Roadmap.md)
- [首次搭建手册（2026-11-18）](docs/First_Session_Manual.md)
- [新手45分钟×14次开发计划](docs/Beginner_14_Session_Plan.md)

## 已归档方案

- [《一纸扬州：盛唐上元夜》旧方案](docs/archive/Yangzhou_Paper_Tang_Vol1_GDD.md)

当前不制作自由城市、战斗、金融经营或复杂多时代时间线。先把“异文调查”做成一段可玩的故事。

## 版本管理

项目使用 Git 和 Git LFS。`main` 保持可打开、可运行；开发工作放在独立分支，通过拉取请求合并。远程仓库默认使用私有可见性。
