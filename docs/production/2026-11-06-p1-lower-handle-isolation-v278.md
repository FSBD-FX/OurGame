# v278 · 下层房间句柄异常的三组隔离对照

对象是 [v276 原始后段错误](../../../验证证据/october13-stable-claw/prologue-post-20261106-102554-662-dd48ff629fe54c8c8b51e97a3715d80a/godot.external.log:502)：原 #6 已读取并实例化过 `LowerBuriedMachinery.tscn`，#11 完成真实恢复，#12 在再次 `GD.Load` 时经 Godot `SwapGCHandleForType` 记录 `Handle is not initialized`。该异常早于 #12 的实例化和遭遇重置，不能归因给战斗规则。它不是 [v277 小地图候选错误](../../../验证证据/october13-stable-claw/thruster-minimap-lifecycle-20261106-105511-368-6fe803405d1d43e4b5c913f1fc3954e3/godot.external.log:128) 的同一调用点：后者在 `ResidentSceneBinding.Instantiate`，同一个原生桥不等于同一个句柄或同一种触发条件。

## 执行与结果

新入口只在测试 `RunTask9Tests.cs` 和受保护 PowerShell 宿主：`-Phase p1-lower-handle -P1LowerHandleArm <arm> -SourceRows 1898`。每组一个独立无窗口进程及隔离证据目录，用原注册表索引和完整名称选择原委托，沿用默认缓存、原释放尾帧和相同的测试回退设置。没有插入中间等待、GC、额外保活引用或崩溃采集。保留原 stderr、stdout 和 Godot 合并日志；两个输出通道须有完整且有序的收据，已知 ERROR 或退出非零会否决。直接根节点身份不变不是原生资源释放证明。

| 隔离组 | 实际运行顺序 | 原生结果 | `G:/knight/验证证据/october13-stable-claw` 目录 |
| --- | --- | --- | --- |
| `lower-only` | #12 下层遭遇加载／击倒一员／重置两员 | 868 ms，退出 0，未见诊断 | `p1-lower-handle-20261106-113718-963-4faebdb0ef73428ea08f8608f91d713c` |
| `reload` | #6 结构加载并释放 → #12 | 775 ms，退出 0，未见诊断 | `p1-lower-handle-20261106-113853-459-89b9d8f81f3443e39f0b3c70ebd70064` |
| `recovery-reload` | #6 → #11 真实恢复及原释放 → #12 | 4,538 ms，退出 0，未见诊断 | `p1-lower-handle-20261106-114022-218-f976abdf0c504e0b8d2d054e3249d959` |

第三组 #11 确实走完同一角色的原恢复流程，请求／完成各一次，原始等待 10 物理帧；#11 返回与 #12 开始同为 `physics=32 process=4`，没有插帧。三组同一 1,898 行源码 `B095EEB16435BB5892FB12502007B690244E4AA8AAF7CDC5117091CCA063A1D4`／二进制 `CA469751A3C05AF83E54ED268C90642F65CF7C68D2619FC505815A20762BC9CC`。宿主解析器另用 3 个正例、9 个负例核对；负例涵盖非零退出、Handle 错误、外部警告、缺通道、错误组、重复、丢恢复见证、插帧及伪普通后段完成标记。独立审查无严重／重要问题；静态提醒解析器未单列 `SCRIPT ERROR:` 与非标准位置的 `FATAL:`，本次三份原始通道额外复扫均无这些文本。

## 普通回归中的新证据

- `actions-20261106-114339-312-7a56f6282e134f258922798caee275eb` 通过（9,603 ms），含已保存 Player 的键盘动作与局部空中重按拒绝；不是双路线全程凭证。
- `post-boundary-contract-20261106-114439-866-462e0da20e0d4906ac2e87126f199935` 通过（861 ms），原同步/异步边界及主错误保留合同仍成立。
- `prologue-post-20261106-114556-486-3211f39f7b0c4a7591ec939a8fa73823` **严格失败**（206,104 ms，原生退出 0，75 项均执行，未超时）。原 #12 在同一帧紧接 #11 并正常返回；本次错误在 #18 `MountedGuidanceRendersAndClearsWithoutHidingObjective`，新夹具首次启动、尚未发 `MOUNTED_GUIDANCE_PREPARATION_FACTS` 时。栈为 `Prepare→Reconcile→TrySpawnCore→ResidentSceneBinding.Instantiate→PackedScene.Instantiate→SwapGCHandleForType`，与 v277 小地图候选的实例化栈一致，随后真实 `Playing` 和用例返回均被记录。这证明同样错误可发生在本夹具第一次启动，不需在此夹具先重开；不能排除进程更早执行历史的影响，更不能推出具体坏句柄属于哪只居民或哪个子资源。[本次原始错误](../../../验证证据/october13-stable-claw/prologue-post-20261106-114556-486-3211f39f7b0c4a7591ec939a8fa73823/godot.external.log:516)

六案同一源码/二进制且与当前逐行相符，六个独立 PID 终态，30 对保护清单和 36 份封存二进制一致，每案 109 条用户保护记录未变。相较 v277 只有一个测试 C# 输入改变，外加宿主新增独立阶段；无正式玩法、场景或引擎改动。没有把旧技术通过拼成当前正式通关验收。

## 能与不能得出的结论

三组都只跑一次，说明所列短序列可执行，本轮未重现已知故障。不说明完整前缀是必要条件，不排除句柄错误与负载/生命周期有关，也不证明恢复本身导致或修复了它。省略完整后段的前置执行及其主场景保活是本对照边界。旧错误与严格失败保留。

另一条居民实例化案：v277 原错误在 `ContinueOnlyDurableMap` 完成和 `NewGameClearsMap` 完成之间，其间两次 `StartNew`，不足以确定哪一次。v278 复发给出明确的首次启动见证，故下一诊断优先用原 #18 首次启动／同一启动夹具加已知前序做有区别的隔离；小地图分支仍可用原 `NewGameClearsMap` 与 `ContinueOnlyDurableMap→NewGameClearsMap` 区分首次与重开。现有实例化前后只记 manager/epoch/槽与加载边界的标量，复发才关联身份；不采集额外崩溃，不改变资源生命周期，不连续重跑求安静。

路线验收可并行推进：在原 `RunTask10` 内以 `-1/+1` 观察实际 Motor 的同一物理步、角色身份、输入序号和 `AcceptedJumpSequence`。真实支撑只提供一次普通起跳许可；离地后的土狼首跳要使用未消耗许可，落地缓冲必须经过真实落地，弹板和爪助推不算普通跳。分别用真实 Space 的土狼、缓冲及一次地面起跳后短时间空中重按对照验证探针，随后在双路线实际运行核对无采样缺口。门禁/普通墙面另有独立证据线，不借跳跃观察代签。此处仅记录已有代码可行性，本轮尚未实现或运行路线观察。
