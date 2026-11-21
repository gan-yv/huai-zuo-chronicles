# World RPG Project Template

当前稳定模板：

- Git 分支：`template/world-rpg-base`
- 版本标签：`template-world-rpg-v1.1`
- 来源版本：`demo02-world-navigation-v0.13`

## 已包含

- Unity 6000.5.7f1 URP 工程。
- Boot 标题场景、开始、继续、暂停和返回标题。
- 世界运行时、场景加载、出生点和区域触发。
- 第三人称移动、镜头防穿墙和角色切换。
- NPC 对话、调查交互和任务导航罗盘。
- 现世/旧时时间层与可复用锚点。
- 基础战斗模块，可作为可选内容保留或删除。
- JSON 存档、自动检查点、`F5` 保存和 `F9` 读取。
- Git LFS、忽略规则、CHANGELOG 和协作规范。
- 广陵灰盒场景作为第一个内容实例。

## 创建新项目

推荐使用一键脚本：

```powershell
.\tools\new-project-from-template.ps1 `
  -Destination "D:\GameProjects\NewWorld" `
  -Minimal
```

脚本会：

1. 从本仓库的模板分支创建一个新检出。
2. 将新项目分支重命名为 `main`。
3. 移除模板仓库的远程地址。
4. `-Minimal` 会删除 `demo01/` 和 Demo02 的早期归档。
5. `-Minimal` 会生成新项目入口 README，并创建一条干净的初始化提交。

如果已有新 GitHub 仓库，可以同时传入：

```powershell
.\tools\new-project-from-template.ps1 `
  -Destination "D:\GameProjects\NewWorld" `
  -Minimal `
  -NewRemoteUrl "https://github.com/用户名/新仓库.git"
```

## 手动创建

```powershell
git fetch origin
git clone --branch template/world-rpg-base --single-branch <仓库地址> NewWorld
cd NewWorld
git switch -c main
git remote remove origin
```

## 新区域接入步骤

1. 复制 `Greybox` 场景作为新区域的基础。
2. 替换环境根节点和区域内容。
3. 至少添加一个唯一 `WorldSpawnPoint`。
4. 使用 `WorldArea` 标记街区范围。
5. 使用 `WorldPortal` 配置目标场景和出生点 ID。
6. 把新场景加入 Build Settings。
7. 完成后提交并创建区域版本标签。

## 模板升级规则

- 模板分支用于保存稳定基线，不在模板分支继续开发内容。
- 新项目从模板分支创建独立分支。
- 通用系统改进先进入正常开发分支，验证后再更新模板标签。
- 不把项目专属剧情、角色和美术误加入通用模板。
