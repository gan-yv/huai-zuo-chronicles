# Huai-Zuo Chronicles

Unity 3D 城市探索与技术策划作品集项目。

当前版本为可运行灰盒垂直切片，不是商业完成品。重点展示完整玩法闭环、系统拆分、存档、世界内容组织、调试和迭代过程。

![标题界面](demo02/docs/screenshots/title-screen.png)

## 目标岗位

- Unity 客户端 / Gameplay 程序
- 技术策划
- 初级关卡或大世界设计

## 可验证内容

- 第三人称移动、镜头防穿墙和角色切换。
- 可复用的 NPC 对话与街区小故事。
- 线索调查、任务目标与方向距离罗盘。
- 现世/旧时时间层与三个修复锚点。
- 官署前街、旧仓内院、旧仓室内、东关街和彩衣街。
- 街内传送与世界地图快速旅行。
- 可选战斗、攻击预警和闪避无敌帧。
- JSON 存档、自动检查点和标题界面继续游戏。
- 新游戏开场叙事、精简 HUD、F1 操作帮助和完整结局页面。
- 两个街道结局状态与重新开始流程。

## 运行

Windows 构建位于：

```text
demo02/Builds/WindowsDemo/HuaiZuoChronicles.exe
```

发布压缩包位于：

```text
demo02/Builds/Releases/HuaiZuoChronicles_Windows_x64_v0.18.zip
```

[从 GitHub Release 下载 Windows 试玩包](https://github.com/gan-yv/huai-zuo-chronicles/releases/tag/portfolio-v0.18.0)。

构建目录默认不进入 Git，发布包作为 GitHub Release 附件保存。

## 操作

```text
WASD       移动
Shift      奔跑
Ctrl       闪避
Space      跳跃
鼠标左键    近战攻击
鼠标右键    旋转镜头
E          调查、对话、修复
Tab        切换出场角色
Q          切换时间层
M          世界地图与快速传送
Esc        暂停
F5 / F9    保存 / 读取
R          结局或队伍倒下后重开
```

## 技术重点

### 世界运行时和区域

- `WorldRuntime` 保存当前世界、区域和待进入出生点。
- `WorldArea` 用触发器维护街区状态。
- `WorldSpawnPoint` 与 `LocalWorldPortal` 支持区域切换。
- 世界地图保留当前调查、队伍和时间层状态。

### 存档

- 版本化 JSON 存档。
- 保存场景、位置、队伍、线索、时间层、锚点、战斗状态和结局。
- 支持自动检查点、`F5` 手动保存和 `F9` 读取。

### 内容系统

- NPC 对白采用逐句数据，不依赖专用过场。
- 时间层通过可见性和碰撞切换管理。
- 灰盒场景由可重复运行的 Editor Builder 生成，降低手工维护成本。

## 已知限制

- 美术仍为灰色占位与基础颜色。
- 界面主要使用原型级 OnGUI。
- 车辆、人群、正式音频和 VFX 尚未制作。
- 大地图目前由街内远距离区域验证，后续需要拆分 Additive Scene。
- 长时间游玩和低配置性能尚未完成系统测试。

## 面试讲述方向

1. 为什么先做垂直切片，而不是继续扩大地图。
2. 如何把战斗与探索主流程解耦。
3. 如何设计可扩展区域、出生点、传送和存档。
4. 如何通过试玩发现交互、镜头和战斗问题并迭代。
