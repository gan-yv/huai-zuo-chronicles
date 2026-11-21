# SourceAssets

本目录保存尚未进入 Unity 的源素材和参考资料。

| 目录 | 内容 |
|---|---|
| `ArtSource/Characters/` | 角色 PSD、雕刻、原画和贴图源文件 |
| `ArtSource/Environment/` | 场景概念图、建筑贴图和地形源文件 |
| `ArtSource/Props/` | 物件、道具和张贴物源文件 |
| `ArtSource/UI/` | 界面设计稿、图标源文件和字体授权 |
| `AudioSource/Music/` | 音乐工程和未压缩母带 |
| `AudioSource/SFX/` | 音效工程和采样源文件 |
| `AudioSource/Voice/` | 配音录音和剪辑工程 |
| `AnimationSource/` | Blender、Maya、动捕和摄影表源文件 |
| `References/` | 参考图、截图和视觉方向资料 |

规则：

- Unity 只读取 `UnityProject/Assets/Demo02/` 中的导出资源。
- 保留可编辑源文件，但不要把同一素材的多个临时版本复制进 Unity。
- 大文件由 Git LFS 管理。
- `References/` 中的图片不代表获得使用授权，不能直接进入正式构建。
