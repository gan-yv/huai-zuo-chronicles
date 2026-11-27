# 荧主角模型接入

![待机、奔跑与攻击动作验收](screenshots/lumine-character-showcase.png)

![广陵正式场景中的荧](screenshots/lumine-gameplay.png)

## 已验证内容

- 模型：22,819 个三角面、312 根骨骼、66 个可导出表情。
- 原始高度约 1.542 米，主角预制体统一缩放到约 1.78 米。
- 原始 PMX 只有 A-pose 绑定姿势，没有附带动作。
- 乱码贴图已按文件内容恢复：`敮→髪`、`堖→衣`、`敡→肌`、
  `尦慺1→元素1`。
- 材质转换为 URP Lit Alpha Clip，保留面部透明层、发色、肤色、服装和元素贴图。

## 动画复用

现有角色链使用 Generic 骨架和离线烘焙动作，不依赖 Unity Humanoid Avatar。
如果直接把荧的 T/A-pose 接到旧控制器，动画路径会全部缺失，角色会保持
绑定姿势并随父节点平移。

本项目改为在构建阶段把 Quaternius 动作采样结果烘焙到荧的 32 根核心骨骼，
包含：

- 待机、步行、奔跑和对话循环。
- 攻击、受击、交互、拾取和死亡。
- 胯部到头部、肩臂、手指、腿和脚趾的骨骼映射。
- 根骨骼位移与非映射的头发、裙带骨骼继承。

游戏运行时直接播放荧自己的动画片段，不增加实时重定向开销。

## 重建入口

Unity 菜单：

```text
Demo02/Art/Lumine/Rebuild Character And Actions
Demo02/Art/Lumine/Capture Showcase
Demo02/Art/Lumine/Capture Gameplay
```

Blender 转换：

```powershell
.tools/blender/blender-4.2.11-windows-x64/blender.exe `
  --background --factory-startup `
  --python demo02/tools/art/import_lumine_pmx.py -- `
  --pmx "demo02/二创/荧.pmx" `
  --texture-root "demo02/二创" `
  --glb "demo02/SourceAssets/Characters/Lumine/VIS_Lumine.glb"
```

自动校验入口包含在 `Demo02GreyboxBuilder.VerifyAllForBatch` 中。

## 授权说明

角色原作权利归 miHoYo，PMX 改造作者在模型注释中署名为观海。素材注释允许
本地修正权重、表情、配色和适度改衣，但禁止二次配布、拆件挪用、商业使用及
若干不当题材。本仓库只记录项目内使用，不代表获得公开分发授权。
