# 首次搭建手册（2026-11-18 晚）

> 已归档：本文记录旧 2D 工程搭建，不用于当前 Demo02。

## 1. 本次只做什么

本次目标：

> 建立本地 Git 仓库、创建私有 GitHub 仓库，并推送当前设计文档。

本次目标不是做 Unity 游戏。仓库稳定后，下一次再创建 Unity 工程。

## 2. 当前环境

已确认：

- 工作目录：`D:\GPTCODEX\CODEX\yangzhougame`
- Git 2.55.0 可用。
- Git LFS 3.7.1 可用。
- Unity 6000.5.7f1 已安装。
- 当前目录尚未初始化 Git。
- GitHub CLI 未安装，本次直接使用 GitHub 网页和 Git Credential Manager。
- 全球 Git 配置已有邮箱：`kid1412chess@gmail.com`
- Git 用户名称尚未设置。

## 3. 开始前检查

- 确认 GitHub 账号可以正常登录。
- 确认当前网络可以访问 GitHub。
- 关闭不必要的 Unity 工程，避免文件占用。
- 打开 PowerShell，进入项目目录：

```powershell
Set-Location "D:\GPTCODEX\CODEX\yangzhougame"
```

## 4. 初始化本地仓库

### 第一步：创建仓库

```powershell
git init -b main
```

### 第二步：设置本仓库作者

把下面的用户名替换为自己的 GitHub 用户名：

```powershell
git config user.name "<你的 GitHub 用户名>"
git config user.email "kid1412chess@gmail.com"
```

该设置在仓库内生效，不修改其他项目。

### 第三步：启用 Git LFS

```powershell
git lfs install --local
```

### 第四步：检查即将提交的文件

```powershell
git add .
git status
```

应当看到：

- `README.md`
- `.gitignore`
- `.gitattributes`
- `demo01/docs/archive/legacy_spring_river/` 下的设计文档

不应看到：

- `Library/`
- `Temp/`
- `Logs/`
- `UserSettings/`
- `.env`
- 任何令牌或密码文件

如果状态列表出现不认识的文件，先不要提交。

### 第五步：首次提交

```powershell
git commit -m "docs: establish design and demo plan"
git tag v0.0-design
```

### 第六步：确认提交

```powershell
git log --oneline -3
git status
```

预期：有一条提交记录，工作区干净。

## 5. 创建 GitHub 私有仓库

打开 GitHub 网页：

1. 登录账号。
2. 点击右上角“+”选择“New repository”。
3. 仓库名填 `chunjiang-yiben`。
4. 可见性选择 **Private**。
5. 不要勾选自动生成 README。
6. 不要额外生成 `.gitignore` 或 License。
7. 点击创建。

不要在网页仓库中再生成一个初始提交，否则本地首次推送会产生冲突。

## 6. 连接远程仓库

复制仓库的 HTTPS 地址，形式类似：

```text
https://github.com/<用户名>/chunjiang-yiben.git
```

在 PowerShell 执行：

```powershell
git remote add origin https://github.com/<用户名>/chunjiang-yiben.git
git remote -v
```

确认 `origin` 地址正确后推送：

```powershell
git push -u origin main
git push origin v0.0-design
```

Windows 可能弹出 Git Credential Manager。选择 GitHub 登录并授权。不要在聊天、Issue 或仓库文件中粘贴个人访问令牌。

## 7. 推送后验证

```powershell
git status
git branch -vv
git log --oneline --decorate -3
git lfs ls-files
```

检查 GitHub 网页：

- 仓库为 Private。
- README 可以查看。
- 四份设计文档完整。
- 最新提交和 `v0.0-design` 标签存在。
- `Library/` 等目录没有出现。

## 8. 在 GitHub 建立 Milestone

创建一个 Milestone：

```text
M0：项目基础
```

描述：

```text
完成本地 Git、私有 GitHub 仓库和可运行的 Unity 2D 工程基础。
```

本次可先创建以下 Issues：

1. `初始化 Unity 2D 工程`
2. `建立标题与空对话场景`
3. `实现书坊背景与两个可点击人物`
4. `实现“待/照”异文比对`
5. `建立 M1 核心灰盒试玩清单`

每项都拆成20～45分钟可执行的小任务。不要把“完成整个 Demo”作为一个 Issue。

## 9. 下一次工作时段

下一次再创建 Unity 工程：

1. 新建分支：

```powershell
git switch -c feature/unity-skeleton
```

2. 在 Unity Hub 中使用 Unity 6000.5.7f1 创建 2D 工程，目录为：

```text
D:\GPTCODEX\CODEX\yangzhougame\demo01\UnityProject
```

3. 打开工程一次，让 Unity 生成基础目录。
4. 确认以下目录会被 Git 跟踪：

- `Assets/`
- `Packages/`
- `ProjectSettings/`

5. 确认以下内容没有进入 Git：

- `Library/`
- `Temp/`
- `Logs/`
- `UserSettings/`

6. 提交：

```powershell
git add demo01/UnityProject/Assets demo01/UnityProject/Packages demo01/UnityProject/ProjectSettings
git status
git commit -m "chore: initialize Unity project"
```

不要在第一次创建工程时同时开始写剧情系统。先保证工程可打开、可提交、可回退。

## 10. 常见问题

### Git 报错“Author identity unknown”

仓库内没有设置作者：

```powershell
git config user.name "<GitHub 用户名>"
git config user.email "kid1412chess@gmail.com"
```

然后重新提交。

### GitHub 自动生成了 README，导致 push 被拒绝

优先删除远程仓库并重建空仓库，风险最低。

也可以在确认远程没有重要内容后执行：

```powershell
git pull --rebase origin main
git push -u origin main
```

不要使用 `git push --force` 处理首次配置问题。

### 登录窗口没有出现

```powershell
git credential-manager configure
git push -u origin main
```

若仍无反应，检查系统 Git Credential Manager 是否已安装。

### LFS 文件没有上传

```powershell
git lfs install --local
git lfs track "*.png"
git add .gitattributes
git lfs ls-files
```

当前文档阶段还没有 LFS 文件。正式接入美术后再重点检查。

### Unity 生成了大量临时文件

先执行：

```powershell
git status
```

如果 `Library/` 被列入跟踪，说明它已经被提交过。停止继续提交，先检查 `.gitignore`，再决定是否从 Git 索引中移除。不要直接删除本地 Unity 工程目录。

## 11. 安全与资产注意

- 不上传账号 Token、密码、私钥和 `.env`。
- 不提交来源不明或授权不清的音乐、字体和 AI 素材。
- 每张外部素材记录来源、许可和用途。
- 未完成项目保持 Private。
- 不把商业插件或购买素材的原始包直接推送到公开仓库。
- 重要资源仍然保留本地备份，GitHub 不是唯一备份。

## 12. 本次完成标准

满足以下全部条件即可结束：

- 本地 `main` 有首次提交。
- GitHub 私有仓库可以访问。
- 设计文档已推送。
- `v0.0-design` 标签存在。
- `git status` 干净。
- GitHub Milestone 与最基本 Issues 已建立。

即使 Unity 还没创建，本次也算成功。不要在最后十分钟临时开始新系统。

## 13. 工作时段记录模板

```text
日期：
实际时长：
本次目标：
实际完成：
卡住的问题：
是否提交并推送：
下一次唯一起点：
```
