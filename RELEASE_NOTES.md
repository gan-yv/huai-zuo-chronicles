# Huai-Zuo Chronicles v1.75.0

发布日期：2026-11-27  
平台：Windows x64 / Unity Project  
类型：3D 时间修复动作冒险垂直切片

## 当前入口

- [上次可玩发布 portfolio-v1.74.4](https://github.com/gan-yv/huai-zuo-chronicles/releases/tag/portfolio-v1.74.4)
- [全部历史版本](https://github.com/gan-yv/huai-zuo-chronicles/releases)
- [项目首页](README.md)
- [完整变更记录](CHANGELOG.md)

本次版本聚焦工程验证基础设施，不改变玩法、存档格式和内容版本。

## 本版重点

- 新增 `Demo02.Tests.PlayMode` 程序集。
- 验证 `Boot` 场景可加载并解析默认章节、案件和运行时内容目录。
- 验证正式游戏场景启动世界运行时、游戏流程、存档系统和玩家队伍。
- 验证所有流式区域可附加加载并卸载，确保内容目录与 Build Settings 一致。
- 架构审计与仓库校验要求 PlayMode 测试程序集持续存在。
- 补充 PlayMode 批处理运行方法和发布门禁说明。

## 验证

- EditMode：12/12 通过。
- PlayMode：3/3 通过。
- 内容版本保持 16，存档架构版本保持 3。

## 已知限制

- 项目仍是垂直切片，内容规模有限。
- 当前 PlayMode 套件是启动冒烟，战斗输入、完整调查流程、长时间运行和性能
  基线将在后续版本继续增加。
