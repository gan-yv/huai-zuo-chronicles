# Huai-Zuo Chronicles v1.75.3

发布日期：2026-11-27  
平台：Windows x64 / Unity Project  
类型：3D 时间修复动作冒险垂直切片

## 当前入口

- [上次可玩发布 portfolio-v1.74.4](https://github.com/gan-yv/huai-zuo-chronicles/releases/tag/portfolio-v1.74.4)
- [全部历史版本](https://github.com/gan-yv/huai-zuo-chronicles/releases)
- [项目首页](README.md)
- [完整变更记录](CHANGELOG.md)

本次版本建立最底层 Foundation 程序集边界，不改变玩法、存档格式和内容版本。

## 本版重点

- 新增 `Demo02.Foundation` 程序集，收纳服务注册、场景常量和启动参数入口。
- Runtime、Editor、EditMode 和 PlayMode 显式依赖 Foundation。
- Foundation 禁止引用任何游戏程序集，依赖方向由架构测试强制检查。
- 保留脚本 GUID，现有场景、Prefab 和构建引用保持兼容。

## 验证

- EditMode：13/13 通过。
- PlayMode：3/3 通过。
- 内容版本保持 16，存档架构版本保持 3。

## 已知限制

- 项目仍是垂直切片，内容规模有限。
- 当前 PlayMode 套件是启动冒烟，战斗输入、完整调查流程、长时间运行和性能
  基线将在后续版本继续增加。
