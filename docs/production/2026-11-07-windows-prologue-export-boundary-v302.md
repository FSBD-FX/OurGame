# v302 · 完整序章 Windows 交付入口核对

推进 D05/Q05。只读核对当前工作区、Git 版本与已有样板的打包方式；没有创建 ZIP、改预设、写入 Git、启动构建或运行 Godot。

- 当前 `export_presets.cfg` 仅有 `Basin Sample Windows x64`，启用 `basin_sample`。`project.godot` 的默认入口为 `res://scenes/prologue/PrologueLevel.tscn`，但该功能标签会改走 `res://scenes/prologue/sample/BasinQualitySample.tscn`，并使用单独的样板用户目录。现有预设导出的不是完整序章。
- `tools/stage-basin-package.ps1` 默认 `SourceCommit=HEAD`，从指定提交用 `git archive` 读取白名单，不读取当前工作区。当前 HEAD `e7c7049` 与工作区的项目配置、双语表、指导模型不一致；正式交互控制器 `scripts/interactions/EnvironmentInteractionController.cs` 在工作区仍为未跟踪文件。把现有 staging 脚本直接用于当前交付会漏掉它和其他未纳入冻结的改动，即便随后导出成功也不代表打包的是当前版本。
- 已定[连续峡谷计划](../superpowers/plans/2026-09-09-continuous-canyon-world.md)要求同版正常窗口路线通过后才增加独立 Windows x64 预设，保留默认完整入口并排除独立样板。该门槛本轮不提前签收。后续还要冻结实际源／资源，核验新解压目录的启动、依赖、隔离存档与正常游玩；不以盆地 ZIP 或旧提交替代，也不发布。

恢复原生复验前重新采样，Windows 可用物理内存约 87–356 MiB，仍频繁换页；没有结束任何程序。当前优先项仍是使用原时限完成同版裂隙、教学与正式阀门的无窗口复验。完整序章不因静态核对缩减。
