# Huai-Zuo Chronicles v1.74.3

发布日期：2026-11-27  
平台：Windows x64 / Unity Project  
类型：3D 时间修复动作冒险垂直切片

## 当前入口

- [GitHub Release](https://github.com/gan-yv/huai-zuo-chronicles/releases/tag/portfolio-v1.74.3)
- [全部历史版本](https://github.com/gan-yv/huai-zuo-chronicles/releases)
- [项目首页](README.md)
- [完整变更记录](CHANGELOG.md)

Windows 构建附件以具体 Release 的 Assets 为准；仓库本身包含可直接打开的完整
Unity 工程。

## 本版重点

### GitHub 维护

- 新增 `Repository Check` GitHub Actions 工作流。
- 自动验证版本文件、ProjectSettings、README、CHANGELOG、程序集边界、
  运行时参数入口和 Markdown 本地链接。
- 新增结构化 Bug 和 Feature Request Issue 模板。
- 新增 Pull Request 检查清单和 Security Policy。

### 构建同步

- 完整重建场景、Prefab、材质引用和内容清单。
- 将生成内容更新到 `Demo02.Runtime` 程序集。
- 修复内容清单版本滞后。
- 提供 Windows x64 构建 ZIP，并验证构建可启动。

### 项目展示

- 重写仓库首页，增加项目定位、核心体验、画面展示、快速开始、工程重点和文档入口。
- 更新作品集和发布说明，移除旧版本链接及过时限制描述。
- 同步 GitHub Description、Homepage、Topics 和逐版本 Release 说明。

### 工程架构

- `Demo02.Runtime`、`Demo02.Editor`、`Demo02.Tests.EditMode` 三个显式程序集。
- 架构审计检查单向依赖、运行时参数入口和版本一致性。
- `GameServices` 提供 Composition Root。
- `IGameFlowState` 隔离暂停/结束状态。
- `IContentProvider` 隔离 Resources 与未来 Addressables 实现。
- `RuntimeLaunchOptions` 统一预览、截图、测试和性能参数。

### 存档与输入

- 存档架构版本升级为 3，支持校验和、备份恢复、逐版本迁移和稳定战斗实体 ID。
- 自动存档采用状态签名变化、2 秒防抖和 8 秒最低写入间隔。
- 修复 `RepairRift` 读档降级软锁。
- Input Actions 支持键鼠、手柄和重绑定。
- 模态界面打开时统一阻塞玩法输入，并显示实际绑定提示。

### UI 与 HUD

- `GameplayUiState` 使用统一模态栈。
- 队伍、精英、战斗、目标、导航、提示、设置、存档、地图和对话等主要界面已迁移 UGUI。
- HUD 每帧只更新已有视觉对象，减少 IMGUI 布局和临时字符串分配。

### 世界与战斗

- 敌人具备追击、盾兵、远程和冲刺重击等行为。
- 精英拥有追击、盾压和远程压制三阶段。
- 流式场景加载失败后指数退避重试，卸载完成前占用加载配额。
- 战斗反馈、目标标记、破防、预警和队伍状态统一显示。

## 验证

- 当前完整 EditMode 回归为 12/12 通过。
- 自动验证覆盖 Architecture、Save、Story、Combat、Data、World 和 InputUi。
- 版本审计确认：

```text
Version audit passed: v1.74.3, content v16, save schema v3.
```

## 已知限制

- 项目仍是垂直切片，内容规模有限。
- 标题、暂停、诊断和少数专用流程仍使用 IMGUI。
- 正式角色、建筑、音乐和 VFX 尚未全部替换。
- Windows 构建资产只存在于附带附件的 Release。

## 版本与维护

- `demo02/version.json` 是版本单一来源。
- 每次修改必须递增版本并更新 `CHANGELOG.md`。
- 可玩版本创建 annotated tag 和独立 GitHub Release 说明。
