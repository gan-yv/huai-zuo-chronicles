# 版本规范

`version.json` 是 Demo02 的单一版本源，记录：

- `version`：面向玩家和构建产物的语义版本。
- `channel`：发布渠道。
- `contentVersion`：剧情与内容数据版本。
- `saveSchemaVersion`：必须与 `WorldSaveData.CurrentVersion` 一致。

## 递增规则

- `PATCH`：缺陷修复、文本修正、不改变数据格式的小调整。
- `MINOR`：新系统、新玩法、架构模块、内容章节或兼容性扩展。
- `MAJOR`：存档或内容格式无法兼容、产品边界发生根本变化。

每次代码、数据或工程结构修改都必须：

1. 更新 `version.json`。
2. 在 `CHANGELOG.md` 增加对应版本标题和条目。
3. 更新 Demo02 README 的当前版本。
4. 运行版本审计和完整测试。

Windows 构建会先执行 `Demo02VersionAudit.VerifyForBatch`，并由
`Demo02VersionInfo` 写入 `PlayerSettings.bundleVersion` 和 `BuildInfo.txt`。
禁止在构建脚本或 UI 中再次写死版本号。
