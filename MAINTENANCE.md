# 仓库维护指南

本仓库以“可运行版本 + 可追溯工程记录”的方式维护。每次修改都应形成独立版本，
而不是在同一个版本中持续累积未记录变更。

## 每次修改

1. 在 `demo02/version.json` 递增语义版本。
2. 在 `CHANGELOG.md` 增加对应版本标题和详细条目。
3. 更新根 README 或 Demo02 README 中受影响的版本、功能和限制。
4. 运行相关 EditMode 测试和 `Demo02VersionAudit`。
5. 检查 Git LFS 文件状态。
6. 创建语义化提交。
7. 创建 annotated tag。
8. 推送分支和标签。
9. 创建或更新 GitHub Release 说明。

详细版本规则见 [demo02/docs/Versioning.md](demo02/docs/Versioning.md)。

## 仓库首页

根 [README.md](README.md) 是 GitHub 首页，必须维护：

- 当前可玩版本与 Release 链接。
- 项目定位和核心玩法。
- 最新截图和可验证功能。
- Unity 版本、运行方式和操作。
- 工程架构与自动验证摘要。
- 当前限制，不把计划中功能写成已完成。
- 文档入口和第三方资源入口。

默认分支必须展示与最新正式版本一致的 README。

## GitHub Release

- 每个 Git 版本标签对应一个 Release。
- Release 正文至少包含版本、提交、日期、更新内容和验证信息。
- 可玩版本应附带 Windows 构建时，明确安装/解压方式、版本和摘要。
- 没有构建附件时必须说明“源码可运行，附件以 Assets 为准”。
- Release 内容来源于 `CHANGELOG.md` 或对应提交的独立变更，不能复制通用模板。

## 测试与质量门禁

发布前至少确认：

- Unity 工程可打开并完成脚本编译。
- `Demo02` EditMode 测试全部通过。
- 版本审计、架构审计和内容审计通过。
- 主流程可从 Boot 进入、保存、重载并完成结局。
- 运行时代码没有新增直接命令行参数读取或编辑器反向依赖。
- 第三方素材来源和许可证可追溯。

## 分支与合并

- `main` 保持可构建、可运行、README 与最新稳定版本一致。
- 功能、修复、文档和素材使用独立分支。
- 合并到 `main` 前必须通过测试和版本审计。
- 不强制推送或重写已公开历史。

## Git LFS

提交前确认：

```powershell
git lfs install
git lfs status
```

图片、音频、模型、视频、字体和压缩包必须由 Git LFS 管理。测试日志、临时截图和
本机构建产物不能进入版本提交。

## 每次维护完成后

- 根 README、Demo02 README、Release Notes 和最新 Release 内容一致。
- GitHub Description、Homepage、Topics 和默认分支正确。
- 所有本地版本标签已推送。
- 每个 Release 都有独立正文。
- 工作区干净，远端提交与标签一致。
