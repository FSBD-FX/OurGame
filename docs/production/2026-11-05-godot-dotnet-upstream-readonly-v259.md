# v259 · Godot/.NET 上游只读核对（非根因确认／非引擎变更）

## 事实

- 当前项目 `Godot.NET.Sdk/4.7.1`、`net9.0`；原生启动日志为Godot4.7.1/a13da4feb。没有在本轮更换引擎、SDK、GC/cache/保活策略或下载执行第三方复现项目。
- 本机无gh CLI，按agent-reach GitHub路线使用现有只读连接器，只访问godotengine/godot公开文件/Issue；没有向GitHub写入、上传日志或发布Issue。
- 已读取当前提交的 [GodotObject.base.cs:80–155](https://github.com/godotengine/godot/blob/a13da4feb/modules/mono/glue/GodotSharp/GodotSharp/Core/GodotObject.base.cs#L80)、[ScriptManagerBridge.cs:1270–1310](https://github.com/godotengine/godot/blob/a13da4feb/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ScriptManagerBridge.cs#L1270) 和 [csharp_script.cpp:1760–1870](https://github.com/godotengine/godot/blob/a13da4feb/modules/mono/csharp_script.cpp#L1760)。NativeInstance是直接托管指针字段；managed target已消失时，SwapGCHandleForType释放旧handle、输出空handle并返回false；当前native refcount_incremented可进入weak→strong转换，disposed_baseref在DEBUG检查handle已释放条件。
- 对应Git blob SHA：base `08aea77e84c7d5a4920170bec1aa30190292d8ea`，Bridge `342a49e70a0252703e516d02a32c9b21de12e8e0`，cpp `6fc8a5edd1f362617406cd89d2c1044d0a08aafa`。这些是取到的源文件blob，不冒称对本机binary进行了源码级等价证明。
- [Issue112067](https://github.com/godotengine/godot/issues/112067) 是相同SwapGCHandleForType症状的公开报告，查询时open、带confirmed/topic:dotnet标签、无已指定修复版本。评论包括custom Resource/缓存/场景装载案例和小复现；[较新的评论](https://github.com/godotengine/godot/issues/112067#issuecomment-6522129923)报告保持场景引用的对照，但这是报告者实验，不是本项目已验证方案。
- [Issue124587](https://github.com/godotengine/godot/issues/124587)描述released weak handle路径，**已于2026-10-30以not_planned关闭**；维护者要求MRP，不能当作已合入修复或经验证根因。仅根据标题搜索结果推定已修复是不可靠的。

## 推理

当前上游实现与本机日志的调用链存在明确重合，说明应把引擎/.NET资源桥接问题作为待区分假设，而非一直归罪于最近的游戏对象或任意一次Dispose。它**不能证明**KNIGHT所有FATAL/无效Profile/无效区域定义都来自同一个上游缺陷，更不能跳过项目自身生命周期误用检查。

本轮场景退休竞态有独立实际RED→GREEN，是项目中已修的一条路径；普通pre仍6Handle、REFpre仍sector finalizer FATAL、post仍缺Profile/FATAL，不能用该修复或上游Issue冲销。

## 风险与下一边界

不凭评论自动加入全局强引用缓存、IgnoreDeep、强制GC、重新注册finalizer、延时保活或替换/修改引擎。这些动作会改变当前政策、时序和内存，并可能掩盖失效引用。任何引擎对照/新runtime或GC-cache保活策略须另行确认范围并保持原版可回退；本轮没有执行。也不下载/执行陌生复现仓库。

继续收齐当前原版同源回归，保留每个失败；与此同时可推进不涉及这些策略的已批准动作连续性修补。没有证实当前某个可用更新版本能解决KNIGHT故障，不承诺“升级即可修复”。
