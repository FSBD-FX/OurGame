# v295 · 前段句柄错误：未替换定义的对照

本轮推进 Q05 的原因排查，未修改游戏、测试或构建器。v294 裂隙专项仍停在编译：已有原 180 秒限时终态，无 Godot 新运行。Windows 当前可用物理内存读取依次见 22、143、172 MiB，系统仍有高频换页；最后一项同时读到提交量 53,136,347,136 bytes、提交上限 68,493,709,312 bytes。物理内存紧张不等于提交限额用尽，也不证明它就是上次超时原因。未启动构建、未停止现有进程、未建立监控或定时任务。

## 原始证据与区分

目录均在 `G:/knight/验证证据/october13-stable-claw`。本轮直接核对三份历史原日志和两份封存源码哈希，不把历史运行当成当前版本通过。

| 证据 | 现场 | 可排除什么 |
| --- | --- | --- |
| `prologue-pre-20261025-073248-668-166cb79b10284d78a6a6458250fff81f`（普通 v110） | `godot.external.log` 16 行先通过低盆地相机，19/21/29/30 行随后记录 `Handle is not initialized → SwapGCHandleForType → Node.AddChild → BasinAscentCameraRuntimeTests.CheckPosition:98`；107 行才首次出现补给 New Game 回执。哈希核对的 `RunPrologueTests.cs` 199/208 确认相机在补给之前；同案 `BasinAscentCameraRuntimeTests.cs` 335–358 实际加载保存的 `PrologueLevel.tscn`，为夹具改出生位置和 `InitialSector`，未换成旧版定义。 | 这类托管错误可以在没有执行补给和旧定义替换的挂载发生。旧版替换并非错误类的必要条件；不同历史案的实际失效对象可能不同。 |
| `prologue-pre-sector-identities-20261021-223341-981-b64bdd47fb0e4f01aadc1193e7fad66b`（v042 诊断案） | `godot.external.log` 76–77 行：盆地实例的五个旧版定义 ID 与替换前五个均不同。81–85 行：补给后，下降区保存源、实例及挂载前仍在五次观测中给出完全一致的五个原 ID，均非空且 `IsInstanceValid`；86 行仍报同类错误。 | 排除本案“补给把后续实例读到的保存定义数组清空／替换掉”解释。有效 ID 不是原生脚本句柄健康证明，也不能排除未观测字段、间接释放或 getter 对时序的扰动。 |
| `sealed-v290-prologue-pre-20261106-191202-399-ccb3e4add56d4907a1306dbc5c2b6713`（普通 v290） | 147–148 行第三次挂载 `CheckSupply → OpenSample → AddChild` 当场报错，紧邻上一轮 Title 补给的清理。更具体的是 158–177 行真实调用栈：当前 `CheckSupply` 上游的同一续调栈仍包含上一用例 `ReturnToTitleDuringHealingStillPersistsTheClaim:524`，即该前用例完成后紧接着启动下一挂载。 | 这能定位此案的调度边界，不证明前一棵根没释放，也不证明追加一帧能修复错误；旧 v110 说明不应把补给 Title 路径当所有案例的共同必要条件。 |

## 下一次实验的约束

恢复可用构建后先完成 v294 原生裂隙专项。Q05 后续优先保留正式保存定义的相机／下降挂载作为非旧版对照，与补给挂载在同一构建下比较。复用现有 mount/cleanup 收据；若须对比前一根清理返回与下一根挂载之间的调度位置，单独作为测试变量，不直接给生产流程加等待。原始诊断不得豁免，比较不复现也不得宣称修复。未更换缓存策略或 GC，未额外采集原生崩溃。
