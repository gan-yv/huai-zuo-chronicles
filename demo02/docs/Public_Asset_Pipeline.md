# Public Asset Pipeline

本项目只接入授权明确、可验证来源的公开美术素材。公开可见或可下载不等于可以
再分发；商业游戏提取资产、来源不明的模型和缺少许可证的转载文件不得进入工程。

## 允许的授权

- CC0：优先使用，可免署名用于商业与非商业项目。
- CC-BY：允许使用，但必须在游戏说明、作品集页面和仓库署名文件中保留作者。
- 明确允许运行时使用的商业素材：保留购买记录与许可证文本。

## 当前自动化流程

下载 Poly Haven 道具：

```powershell
pwsh demo02/tools/assets/fetch-polyhaven-props.ps1
pwsh demo02/tools/assets/fetch-opengameart-music.ps1
```

校验第三方素材路径、许可证和哈希：

```powershell
pwsh demo02/tools/validation/validate-art-assets.ps1
```

下载脚本会完成：

1. 从 Poly Haven API 读取 1K glTF 文件列表。
2. 下载模型、二进制数据和所需贴图。
3. 同时写入 `SourceAssets` 与 Unity 运行资源目录。
4. 为每个资产生成 `SOURCE.md`。
5. 更新 `ASSET_MANIFEST.csv` 中的作者、来源、许可证和 SHA256。

## 当前公开模型

| 资产 | 作者 | 用途 | 许可证 |
|---|---|---|---|
| Chinese Screen Panels | Kirill Sannikov | 茶肆屏风 | CC0-1.0 |
| Chinese Armchair | Kirill Sannikov | 茶席座椅 | CC0-1.0 |
| Chinese Tea Table | Kirill Sannikov | 茶席桌案 | CC0-1.0 |
| Chinese Cabinet | Kirill Sannikov | 旧仓室内陈设 | CC0-1.0 |
| Liyan | elerya | 探索环境音乐 | CC-BY-3.0 |

## 接入原则

- 素材只负责视觉，不把玩法脚本挂到第三方模型深层节点。
- 场景根节点、碰撞和交互仍由项目代码生成，便于替换和回退。
- 所有运行时模型必须能在没有原压缩包的情况下重新导入。
- 不接受来源不明、粉丝转载、游戏解包或缺少许可证记录的模型。
