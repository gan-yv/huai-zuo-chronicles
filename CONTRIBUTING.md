# Git 与工程规范

本仓库同时保存 Demo01 历史原型和 Demo02 当前项目。所有内容必须能通过 Git 追踪，并能在一台新电脑上恢复为相同结构。

## 1. 目录职责

- `demo01/`：旧 2D 原型、构建和归档，不再扩展功能。
- `demo02/`：当前 3D 游戏。目录说明见 [Demo02 README](demo02/README.md)。
- `demo02/UnityProject/Assets/Demo02/`：只放 Unity 实际加载的运行资源。
- `demo02/SourceAssets/`：放 PSD、Blend、音频工程等源文件，导出后才能进入 Unity。
- `demo02/docs/`：当前设定、制作范围和历史归档。
- `demo02/Playtests/`：试玩记录、截图和反馈，不把反馈混进设计文档。
- `demo02/tools/`：可重复执行的构建、资源检查和工程校验脚本。
- `demo02/Builds/`：本机构建产物，默认不提交；目录用途由 README 保留。
- 根目录只放仓库级入口和共享配置。

## 2. 分支

- `main`：稳定、可打开并通过基本检查的版本。
- `feature/<name>`：新玩法或正式功能。
- `fix/<name>`：明确问题的修复。
- `docs/<name>`：设计、剧情和流程文档。
- `art/<name>`：正式美术、音频和界面资源。
- `test/<name>`：一次性实验，通过后再整理进正式分支。

一个任务只使用一个分支。任务未完成时继续在同一分支工作，不把半成品合并进 `main`。

## 3. 提交

使用简单、可检索的前缀：

- `feat:` 新功能或玩法。
- `fix:` 错误修复。
- `docs:` 设计、剧情和流程。
- `art:` 美术、字体和音频。
- `test:` 测试场景、记录和实验。
- `chore:` 工程配置、依赖和构建。

一次提交只解决一类问题。不要用 `update`、`changes` 或无说明的提交信息。

示例：

```text
feat: add third-person movement and camera
fix: prevent camera clipping in narrow streets
docs: define the Guangling vertical slice scope
chore: organize the Demo02 project structure
```

## 4. Git LFS

以下资源必须通过 Git LFS 提交：

- PSD、PSB、AI、PNG、JPG、TIF、TGA 等图片。
- WAV、MP3、OGG、FLAC 等音频。
- FBX、Blend、OBJ、GLB、MA、MB 等模型源文件。
- 视频、字体、压缩包和 Unity 包。

规则已经写入 [.gitattributes](.gitattributes)。首次克隆后运行：

```powershell
git lfs install
git lfs pull
```

如果 `git lfs` 命令不可用，先安装 Git LFS；不要暂时把大文件作为普通 Git 对象提交。

## 5. 不应提交

- `Library/`、`Temp/`、`Logs/`、`UserSettings/`。
- `Builds/` 中的本地测试产物。
- IDE 生成文件、崩溃记录和操作系统缓存。
- `.env`、令牌、密钥、账号配置和私人数据。
- 未经许可的第三方美术、音乐、字体或代码。

第三方资源放入 `demo02/UnityProject/Assets/ThirdParty/` 时，必须同时保存来源、作者、许可证和使用范围。

## 6. 每次提交前

1. 运行 `git status`，逐项确认新增和删除的文件。
2. 确认没有生成目录、构建产物、秘密文件或无关素材。
3. 在 Unity 中打开目标场景并完成相关流程。
4. 运行已有的校验脚本。
5. 检查文档链接和路径。
6. 用一句明确的话描述本次改动。

## 7. 拉取请求

每个拉取请求至少说明：

- 改变了什么体验或工程能力。
- 如何运行和验证。
- 是否修改设计、目录或依赖。
- 已测试的平台与分辨率。
- 截图、视频或试玩记录。
- 已知问题和未完成内容。

合并条件：

- 游戏可以从标题进入目标场景。
- 没有阻断流程的错误。
- 工程可以在另一台电脑上打开。
- 构建产物、生成目录和秘密文件没有被提交。
- 二进制素材通过 Git LFS。

## 8. Issue 与工作粒度

Issue 标签：

- `design`：设定、剧情、玩法与数值。
- `narrative`：对白、任务文本和分支。
- `code`：Unity、脚本与工具。
- `art`：美术、界面与图标。
- `audio`：音乐、音效和声音反馈。
- `playtest`：试玩、反馈与可用性。
- `bug`：阻断、错误和回归。
- `scope-cut`：明确延期或删除。

每个 Issue 应能在一个或几个 45 分钟工作时段内推进。无法在一个时段内验收的任务必须拆分。
