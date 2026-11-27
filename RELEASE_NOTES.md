# Huai-Zuo Chronicles v1.75.1

发布日期：2026-11-27  
平台：Windows x64 / Unity Project  
类型：3D 时间修复动作冒险垂直切片

## 当前入口

- [上次可玩发布 portfolio-v1.74.4](https://github.com/gan-yv/huai-zuo-chronicles/releases/tag/portfolio-v1.74.4)
- [全部历史版本](https://github.com/gan-yv/huai-zuo-chronicles/releases)
- [项目首页](README.md)
- [完整变更记录](CHANGELOG.md)

本次版本统一运行时服务发现入口，不改变玩法、存档格式和内容版本。

## 本版重点

- 新增 `GameServices.Resolve<T>()`，集中处理场景服务解析和缓存。
- 迁移运行时全部直接 `FindAnyObjectByType` 调用到统一服务入口。
- 已销毁 Unity 对象的服务注册会在下一次查询时自动失效。
- 架构审计禁止运行时代码绕过统一入口执行场景类型搜索。
- 增加服务解析、缓存和销毁失效的 EditMode 回归。

## 验证

- EditMode：13/13 通过。
- PlayMode：3/3 通过。
- 内容版本保持 16，存档架构版本保持 3。

## 已知限制

- 项目仍是垂直切片，内容规模有限。
- 当前 PlayMode 套件是启动冒烟，战斗输入、完整调查流程、长时间运行和性能
  基线将在后续版本继续增加。
