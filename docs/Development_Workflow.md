# Git 与 GitHub 协作流程

## 1. 目标

- 本地始终保持一个可以运行的游戏版本。
- 每个玩法、剧情和技术尝试都可以回退。
- 大体积美术、音频和字体不挤压 Git 历史。
- 每个开发阶段能够在 GitHub 上追踪和复盘。
- 个人开发也使用短分支和拉取请求，保留设计决策记录。

## 2. 仓库结构

```text
/
├─ README.md
├─ docs/                 # 当前设计、Demo 规格与开发记录
├─ UnityProject/         # Unity 工程实际建立后放在这里
├─ tools/                # 后续可放内容校验或构建脚本
├─ .gitignore
└─ .gitattributes
```

Unity 尚未创建。建立工程后必须一并提交：

- `Assets/`
- `Packages/`
- `ProjectSettings/`

不提交：

- `Library/`
- `Temp/`
- `Logs/`
- `UserSettings/`
- 本地构建目录
- 未取得授权的美术、音乐和字体
- 任何账号令牌或私密配置

## 3. Git LFS

仓库使用 Git LFS 管理：

- PNG、JPG、PSD、TGA 等图片。
- WAV、MP3、OGG、FLAC 等音频。
- FBX、Blend、OBJ 等三维源文件。
- 视频、字体、压缩包和较大二进制素材。

首次克隆后运行：

```powershell
git lfs install
git lfs pull
```

如果 GitHub 仓库创建后 LFS 上传失败，先检查远程仓库的 LFS 配额，不要直接把大文件转成普通 Git 对象。

## 4. 分支规则

- `main`：稳定、可打开、通过基本检查的版本。
- `feature/<name>`：新玩法或正式功能。
- `test/<name>`：一次性玩法实验，通过后再整理进正式分支。
- `fix/<name>`：明确问题的修复。
- `docs/<name>`：设计文档与流程更新。

单个任务超过一次45分钟工作时段仍未完成时，继续使用同一分支，不把未完成状态合并进 `main`。

## 5. 提交信息

使用简单、可检索的前缀：

- `docs:` 设计、剧情和流程文档。
- `feat:` 新功能或新玩法。
- `fix:` 修复错误。
- `art:` 美术、字体和音频资源。
- `test:` 测试场景、测试记录和实验。
- `chore:` 工程配置、依赖和构建。

示例：

```text
docs: define the first manuscript variant
feat: add dialogue runner for the bookshop scene
fix: prevent comparison choice from being confirmed twice
```

## 6. 拉取请求

个人开发也建议创建拉取请求，哪怕不邀请他人审查。

每个拉取请求至少写清：

- 这一段改变了什么体验。
- 玩家如何进入并结束该流程。
- 是否改变了设计文档。
- 已测试的分辨率和构建平台。
- 截图或短视频。
- 已知问题与未完成内容。

合并条件：

- 游戏可以从标题进入目标场景。
- 不存在阻断流程的错误。
- `Library/`、构建产物和秘密文件没有被提交。
- 新增二进制素材已通过 Git LFS。

## 7. GitHub 里程碑

### M0：项目基础

- 仓库、忽略规则和 LFS。
- Unity 工程可以打开。
- 标题画面可以运行。

### M1：核心灰盒

- 书坊单场景。
- 两人调查二选一。
- “待/照”异文比对。
- 一个结果和重新开始。
- 完成首次核心玩法测试。

### M2：Demo v0.1

- 8～12分钟完整流程。
- 修复室、书坊和结果场景。
- 两个结果分支。
- Windows 64 位测试构建。

### M3：试玩修正

- 至少3名目标玩家试玩。
- 记录理解度、选择理由和阻断问题。
- 根据反馈删减或重做核心交互。

### M4：正式美术切片

- 3张正式背景。
- 2位NPC正式立绘。
- 正式书稿界面。
- 基础音效与氛围声。

## 8. Issue 分类

建议使用以下标签：

- `design`：设定、剧情、玩法与数值。
- `narrative`：对白、异文和分支文本。
- `code`：Unity、脚本与工具。
- `art`：背景、立绘、UI和图标。
- `audio`：音乐、音效和声音反馈。
- `playtest`：试玩、反馈和可用性问题。
- `bug`：阻断、错误和回归。
- `scope-cut`：明确延期或删除的内容。

每个 Issue 必须能在一个或几个45分钟时段内推进。无法完成的大任务要拆成可验收的小项。

## 9. 每日流程

1. 从当前里程碑挑一个最小 Issue。
2. 在该 Issue 对应分支工作。
3. 完成后运行 Unity 并走一遍相关流程。
4. 提交并推送。
5. 更新 Issue 或试玩记录。
6. 不在同一天同时开启三个新系统。

## 10. 远程仓库

拟用私有仓库：

```text
chunjiang-yiben
```

远程地址、GitHub 用户名和最终仓库可见性在 GitHub 登录确认后设置。远程仓库建立后，运行：

```powershell
git remote add origin <repository-url>
git push -u origin main
git push --tags
```

不要在没有确认的情况下把项目改为公开仓库。
