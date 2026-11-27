# 角色动作管线

## 荧

- 原始 PMX 只有 A-pose 绑定姿势，没有动作。
- 预制体：`Assets/Demo02/Art/Characters/Lumine/VIS_Lumine.prefab`
- 控制器：`Assets/Demo02/Art/Characters/Lumine/AC_LumineCharacter.controller`
- 动作：待机、步行、奔跑、起跳、下落、落地、攻击、受击、交互、
  拾取、死亡和交谈。
- 32 根核心骨骼在构建阶段从 Quaternius 动作离线烘焙；运行时直接播放荧
  自己的 Generic 动画片段。
- 动作结束后根据当前速度直接回到待机、步行或奔跑。
- 自动眨眼，并在对白期间交替使用 `あ/い/う/え/お` 嘴型。

## 阿鱼

- 原始 VRM/GLB 没有动作。
- 预制体：`Assets/Demo02/Art/Characters/AnimeFishPilot/VIS_AnimeFish_Work.prefab`
- 控制器：`Assets/Demo02/Art/Characters/AnimeFishPilot/AC_AnimeFish.controller`
- 动作：待机、步行、奔跑、交谈和警戒反应。
- `WorkFishCompanion` 的移动由根节点负责，`ReadyMadeCharacterAnimator`
  将实际跟随速度写入 `Speed`；对话系统将 `Talking` 状态写入控制器。
- 尾巴的轻微摆动仍由程序化逻辑叠加，不与上半身动作冲突。
- 自动眨眼，并在对白期间交替使用 VRM 的 `aa/ih/ou/ee/oh` 嘴型。

## 验收

```text
Demo02/Art/Lumine/Rebuild Character And Actions
Demo02/Art/Lumine/Capture Showcase
Demo02/Art/Build Anime Fish Pilot
```

完整回归验证入口为
`Demo02.Editor.Demo02GreyboxBuilder.VerifyAllForBatch`。
