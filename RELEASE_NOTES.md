# Huai-Zuo Chronicles v1.75.2

发布日期：2026-11-27  
平台：Windows x64 / Unity Project  
类型：3D 时间修复动作冒险垂直切片

## 当前入口

- [上次可玩发布 portfolio-v1.74.4](https://github.com/gan-yv/huai-zuo-chronicles/releases/tag/portfolio-v1.74.4)
- [全部历史版本](https://github.com/gan-yv/huai-zuo-chronicles/releases)
- [项目首页](README.md)
- [完整变更记录](CHANGELOG.md)

本次版本用稳定世界对象 ID 替代运行时字符串对象查找，不改变玩法、存档格式和
内容版本。

## 本版重点

- 新增 `WorldObjectId` 和 `WorldRuntime` 世界对象注册表。
- 任务导航改用稳定对象 ID，不再依赖 NPC、修复点和结局选项的 GameObject 名称。
- 地图快速旅行改用出生点注册表，不再使用 `GameObject.Find`。
- 场景装配器为导航对象配置稳定 ID，重复或缺失 ID 会由内容回归发现。
- 架构审计禁止运行时源码继续使用 `GameObject.Find`。

## 验证

- EditMode：13/13 通过。
- PlayMode：3/3 通过。
- 内容版本保持 16，存档架构版本保持 3。

## 已知限制

- 项目仍是垂直切片，内容规模有限。
- 当前 PlayMode 套件是启动冒烟，战斗输入、完整调查流程、长时间运行和性能
  基线将在后续版本继续增加。
