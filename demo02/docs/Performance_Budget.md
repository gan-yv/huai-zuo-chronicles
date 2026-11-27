# 运行时性能与资源预算

## 构建策略

`RuntimeAssetBudgetSetup` 将环境贴图分为三类：

| 用途 | 最大尺寸 | 导入设置 |
|---|---:|---|
| BaseColor / Diffuse | 1024 | CompressedHQ、流式 Mipmap、4× Aniso |
| Normal | 1024 | CompressedHQ、流式 Mipmap、4× Aniso |
| Roughness / ORM / Metallic | 512 | CompressedHQ、流式 Mipmap、4× Aniso |

角色和面部贴图不经过该规则，保留更高细节。预算步骤位于环境生成之后，
避免被环境导入器重新覆盖为 2K。

运行时启用：

- 纹理流式 Mipmap。
- 固定上限约 768 MB 的显存纹理预算。
- 最多降低 2 级 Mipmap。
- 最多 8 个异步纹理请求。
- 4× 各向异性过滤。

## 优化结果

同一设备、1920×1080：

| 指标 | 优化前 | 优化后 |
|---|---:|---:|
| Windows 包体 | 约 816.7 MB | 约 728.6 MB |
| 平均 FPS（30 秒） | 185.9 | 210.5 |
| 1% Low FPS | 120.0 | 131.8 |
| 每 10 秒 GC 次数 | 48 | 约 40 |
| 基准时托管使用 | 约 4.8 MB | 约 5.1 MB |
| 原生总分配 | 约 304.5 MB | 约 242.4 MB |
| 原生总保留 | 约 436.0 MB | 约 360.7 MB |

包体减少约 88 MB（约 11%）。23 个合并后的环境网格在最终场景生成完成后
关闭 CPU 可读副本，运行时原生分配减少约 62 MB。最终帧率使用 30 秒采样，
降低单次短基准的波动影响。

基准文件：

```text
docs/benchmarks/v1.60-memory-baseline.json
docs/benchmarks/v1.60-memory-optimized.json
```

## 流式区域预算

每个附加场景在内容验证阶段输出并检查以下指标：

| 指标 | 单区域上限 | 盐仓增援 | 东关巡哨 | 彩衣望风 |
|---|---:|---:|---:|---:|
| 对象 | 750 | 66 | 129 | 130 |
| 渲染器 | 300 | 2 | 3 | 3 |
| 独立材质 | 24 | 2 | 3 | 3 |
| 顶点 | 1,000,000 | 1,053 | 2,082 | 2,082 |
| 三角形 | 1,000,000 | 1,616 | 3,220 | 3,220 |

区域配置位于 `Assets/Demo02/Data/WorldStreaming/STREAM_*.asset`。导演缓存玩家队伍，
附加场景加载后只缓存本地遭遇和事件，卸载检查不再遍历全场景。

## 诊断接口

`RuntimeDiagnostics` 的基准报告现在包含：

- 平均、1% Low、最低 FPS 和最差帧。
- 原生总分配与保留内存。
- Mono 堆、Mono 使用量与临时分配器。
- GC 0/1/2 次数。
- 图形设备与实际运行分辨率。

运行：

```powershell
demo02/Builds/WindowsDemo/HuaiZuoChronicles.exe `
  --benchmark-seconds=10 `
  "--benchmark-output=D:\benchmark.json" `
  --benchmark-exit
```

## 后续扩展预算

新增章节进入内容管线时，应同时声明：

- 纹理最大尺寸和颜色空间。
- 是否需要流式 Mipmap。
- 场景的常驻网格与材质批次上限。
- 新场景是否可以异步加载。
- 对应的基准镜头与允许波动范围。
- 对应的 `WorldStreamingRegionDefinition` 及单区域预算。

这些规则通过 `IDemo02ContentModule.Validate()` 检查，避免内容增加后悄悄突破
现有内存预算。
