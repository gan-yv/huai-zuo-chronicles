# Huai-Zuo Chronicles v1.75.4

发布日期：2026-11-27  
平台：Windows x64 / Unity Project  
类型：3D 时间修复动作冒险垂直切片

## 当前入口

- [上次可玩发布 portfolio-v1.74.4](https://github.com/gan-yv/huai-zuo-chronicles/releases/tag/portfolio-v1.74.4)
- [全部历史版本](https://github.com/gan-yv/huai-zuo-chronicles/releases)
- [项目首页](README.md)
- [完整变更记录](CHANGELOG.md)

本次版本收口运行时单例生命周期，不改变玩法、存档格式和内容版本。

## 本版重点

- 18 个运行时单例组件在创建时注册、销毁时解除注册。
- 跨类型静态 `Instance` 的 141 处读取迁移到统一服务解析。
- 架构审计禁止新增跨类型静态单例访问。
- PlayMode 验证世界、流程、转场、存档和街区内容服务注册。

## 验证

- EditMode：13/13 通过。
- PlayMode：3/3 通过。
- 内容版本保持 16，存档架构版本保持 3。

## 已知限制

- 项目仍是垂直切片，内容规模有限。
- 当前 PlayMode 套件是启动冒烟，战斗输入、完整调查流程、长时间运行和性能
  基线将在后续版本继续增加。
