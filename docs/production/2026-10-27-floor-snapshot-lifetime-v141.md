# v141 · 测试碰撞快照释放与失败清理

日期：2026-10-27。工程 **KNIGHT / G:/knight/Knight / Godot.NET**。这是原十一项第9项的有界稳定性工作记录，不是新发行版本、V1重打包或GitHub发布，也不是美术升级。

## 结果与未完成项

- 用户决定保持**敌人之间角度优先**；此前近敌与其后方地形的规则、八方向、射程、遮挡和禁抓条件不变。
- 修正自动试玩的 `DescribeFloorCollider`：只返回文字的读取结束后，及时释放取得的临时碰撞快照，不再让这类临时对象等待GC。不是修改玩家的碰撞、跳跃或机械爪。
- 新原生夹具证明了该边界的RED→GREEN；唯一审查发现失败清理存在另一个潜在竞态，已在同一次C/I窗口修正，并用安全协议重新取得原问题RED→GREEN。
- 最终同源八项：新专项、models、keyboard、机关、完整pre、完整post **6 PASS**；完整Main、Explore **2 FAIL**。不能宣称整套全绿或完整序章完成。
- 同轮早先pre确实发生 `gchandle.is_released()` / Finalize FATAL。最新pre/post通过不证明它或Main104200历史FATAL的根因已修复。

原十一项继续 **ACTIVE**；用户当前显示结论仍是NOTSEEN。正式美术、连续动作、音频、实际显示/雾下像素、正常速度与目标硬件、真人游玩和发行验收没有因本轮测试获得完成资格。

## 事实、推理、风险

### 事实

实际CLI为 `4.7.1.stable.mono.official.a13da4feb`；项目使用的GodotSharp与捆绑Release API相同，SHA为 `7B1D06575DB78E4EA4353186BE5ADD207E43617B836DB24D65EC4088E6FAD18C`。先前只读PDB/IL证明返回值经过Ref封送，Dispose会进入原生释放；不能仅按getter名称判定“借用”或“独占”。

精确版本的[原生CharacterBody2D实现](https://github.com/godotengine/godot/blob/a13da4feb/scene/2d/physics/character_body_2d.cpp#L517)在缓存碰撞快照已有额外引用时创建另一份快照。H1实际主线canary在两个原读取点均观测到不同原生对象/托管包装，先持有的引用仍存活。因此**否定“原两个using会使已持有alias失效”的假设**，没有删除它们。临时canary已撤回，其保存源码仍在113838案中。

实际存在的局部边界是另一个非释放读取：`DescribeFloorCollider`取得快照、返回字符串，没有把快照交给调用方。原生小夹具包含真实地板、胶囊和MoveAndSlide接触，验证airborne零次/地板一次读取、准确地板及法线、原有peer引用保持，随后同步释放自己的body，再检查该次实际快照ID。期间三个GC计数均不变。没有用别处再取的快照冒充helper对象，也没有强制GC。

### 推理与最小修正

安全协议下，f122案只有移除该helper的`using`才出现临时快照在body释放后仍存活；恢复六字节`using `后，08663案同一契约通过。证明的是**我们的辅助读取的临时资源延迟存活及确定性释放**，不是永久泄漏、引擎整体所有权安全或历史FATAL归因。

唯一审查C0/I1/M0。I1指出：测量后再按标量ID调用InstanceFromId并释放，可能与自然GC/Finalize竞争；测量期GC计数和瞬时refcount1不能充当清理期同步。旧e98/627原始结果保留，但其清理资格不再单独用于认证。

同一次修正窗口改为：测试专用回调只保存ObjectID与短托管WeakReference，**测量期间不保留强包装引用**；清理期只提升原始弱目标，验证精确ID/refcount1，不按ID重建包装。弱目标丢失而原生对象仍在时，拒绝释放并报告清理未认证；不把它当成功。所有自有清理动作仍尝试执行，主错误与首个清理错误分开保留。

确定性负控在明确持有自有控制对象时清空短弱引用，验证“失去清理资格不得凭有效ID释放”；另有有效lease、错ID、主错＋清理错、单独主错、单独清理错和全部成功控制。它们是**模拟的所有权边界/实际清理协调器测试**，不是伪称复现了原生GC竞态。bc703先得到有效WEAK_LEASE_RED，3aea通过；随后f122→08663用安全协议重新证明原helper问题。没有第二次代码审查、forcedGC、NoGCRegion、复活弱引用、静态Godot强引用、drain或错误过滤改动。

### 风险与保留资格

- Main104200五个unsafe对象没有出生站点ID；本轮不能反向分配其归属。RID15、GCHandle及偶发FATAL继续OPEN。
- 当前两条完整路线失败，不是“本轮已通关但没展示”。Main在后续枪手战E0死亡，合法死亡恢复触发零恢复路线门；不是新证实了生产传送bug。Explore仍在背向防守移动后无法完整登台。
- pre最新通过，但同轮122056案在首个BasinAscentCamera清理附近24秒时确实FATAL。不能用最后一次通过翻写前次失败或宣称因果修复。
- post原四条按来源限定的负控WARNING仍保留，不写成“所有日志零警告”。197966ms使用既有300000ms完整post预算；原180000ms性能资格没有取得，本轮没有调预算。
- 新夹具不加载正常玩家场景；它不能替代完整路线、GPU/像素、实际人眼显示、音频或手感验收。

## 原生案例索引

统一根：`G:/knight/验证证据/october13-stable-claw/`。表中时间为目录内20261027后的时间字段，ID为唯一UUID前八位；准确完整目录、源码/辅助脚本/二进制合同及三份原始日志以各案`provenance.json`为准。全部21案已结束，无timeout或guard失败，未使用ExpectedFailure豁免。各次构建均0错误/0警告。

| 阶段／时间／ID | 严格结果 | 原生ms | 边界 |
| --- | --- | ---: | --- |
| Main 113838 e17668f4 | FAIL | 118363 | H1两个alias均保持，随后枪手死亡；1855源。 |
| Snapshot 121025 e98fd081 | FAIL | 427 | 初次LIFETIME_RED；旧ID重取清理资格后被I1指出不足。 |
| Snapshot 121200 627db721 | PASS | 524 | 初次helper using通过；同样不单独认证旧清理协议。 |
| Main 121428 44f46fa6 | FAIL | 118699 | 原枪手死亡复位，三sink无FATAL/泄漏。 |
| Explore 121657 be7af104 | FAIL | 83638 | 原Walk2760登台失败。 |
| Models 121851 6a8551df | PASS | 416 | 当前模型。 |
| Keyboard 121920 19f09132 | PASS | 11898 | 引擎键盘事件，非OS/真人。 |
| Recycling 122000 2690a32b | PASS | 28055 | 原机关与引导控制。 |
| Pre 122056 b9cdf2ce | FAIL | 24453 | 原生-1073741795，gchandle/Finalize FATAL；没有阶段完成。 |
| Post 122151 bc21b152 | PASS | 200160 | 原75＋owner8；保留四WARNING。 |
| Snapshot 124254 bc7033fd | FAIL | 531 | 确定性WEAK_LEASE_RED，不是实际GC竞态重现。 |
| Snapshot 124502 3aea4d66 | PASS | 525 | I1安全清理、错误优先级及原helper通过。 |
| Snapshot 124626 f12276a8 | FAIL | 409 | 安全协议重证LIFETIME_RED；临时无using，清理OK。 |
| Snapshot 124834 08663a38 | PASS | 409 | 恢复using，原生快照消失、peer有效、无GC、三sink0诊断。 |
| Main 124904 a0a3aeda | FAIL | 118576 | 同枪手死亡，三sink仅位置复位断言。 |
| Explore 125134 346a1f3e | FAIL | 84111 | 同登台失败，位置(2672.0747,4079.9868)。 |
| Models 125330 6465379d | PASS | 515 | 三sink0诊断。 |
| Keyboard 125401 52de659f | PASS | 11899 | 三sink0诊断。 |
| Recycling 125442 4d5d85e9 | PASS | 28214 | 三sink0诊断，旧控制保留。 |
| Pre 125539 7f57b609 | PASS | 154323 | 完整pre与退出drain，三sink0诊断。 |
| Post 125842 c4a87cc6 | PASS | 197966 | 原75＋owner8、完整post/drain；同四原负控WARNING。 |

合计12个严格PASS、9个严格FAIL；预期RED仍记录为失败案例，不改成整案通过。最后同源八案为6PASS/2FAIL，不把不同源码epoch拼成一次“全绿”。

## 唯一审查、裁决与有界保管

唯一最终审查的三个未判定项都继续保留：历史资源因果不归功本修正；v140/生产机制/angle/正式美术与硬件不在本次审查资格内；完整路线及整关资格不成立。I1只在一次TDD修正窗口处理，没有二审，也没有趁机修旧20项Minor或v140的M1。

两次独立只读custody各自覆盖不同epoch：

- 首10案，截止2026-10-27T04:40:13.8523905Z：50对清单、5002份选定实体、60份封存二进制一致。H1旧canonical与随后版本分开保留；20个原owned创建身份已结束，一个数字PID复用未触碰。
- 后11案，截止2026-10-27T05:10:29.3777861Z：55对清单、5374份选定实体、66份封存二进制一致。22个owned创建身份已结束，未声明其他Godot进程空闲。
- 合计105对、10376份选定实体复制、126份封存二进制；不是全历史或完整离线资产包审计，旧缺份没有补造。
- 3aea与最终八案同源、同二进制。当前1856源/896C#全SHA匹配；最终helper联集11路径及Debug6匹配。相对v140b新增一个测试fixture、改Main helper和runner两个旧C#，另改canonical；I1相对bc21只改Main/fixture和canonical一条receipt，runner不变。
- f122→08663完整字节恰好插入`using `六bytes；没有其他字节或CR差异。初次e98→627另有同一行一个CR规范化，旧事实仍保留。

最终source清单SHA：`A8A4EEE112CB378FDA242BE97A72289AE44351D50F97D4A334ECF8EB89B0B648`。

最终binary清单SHA：`7DCFE35B1CC138EF978690FC3C1AD1B34A2177086340974A69C6677BFB48E64A`；Knight.dll为`C113E4C08352EFA89C87FB2EB701C0FDEAEAE73BF0B8B448BD291ACED339C08F`。

最终canonical SHA：`72021DB1A6B554EECD23E399CBF8F0D88769D3DC9730E7671ECBAFD1C4F8A181`。前面的`6E62594F…1324`、`F456C55C…10D3`属于实际保存的旧epoch，不强改为当前。

HEAD仍为`e7c70498bb16ae149a8b6a9d6dc6c1df5ab043f6`、分支`codex/task6-certified-final-20260902T0555`、index SHA仍为`21610FDC8067943E5DF6792D3B5962FFC9DC6565C7C6CFD27A837CC0FBFF945C`。根代理另外在2026-10-27T05:12:04.3853633Z核对最后六份封存/活动二进制及index一致。审计后只更新状态文档，没有再次改源、helper或bin。

## 文件、留档、下一步

本轮代码范围：`tests/integration/PrologueCanyonGeneratedMainRouteTests.cs`、新增`tests/integration/CollisionSnapshotLifetimeRuntimeTests.cs`、`tests/RunThrusterCoreTests.cs`，及既有canonical验证脚本。生产文件、143实体、9残片、76普通敌人及原规则未改；新增静态测试类不新增UID。

没有新图声资源、过程图稿/实录、付费生成、前台窗口、系统键鼠、自动化、Git写入/发布、删除或真实存档语义读取。手动媒体检查2026-10-27T05:12:03.9090442Z：1431609字节/16文件，较3508字节基线增长1428101，Notify=false；没有初始化/重置基线或建立任务。

后续候选来自只读侦察，**尚未写入、未经实测**：

1. Explore：在PistonOne仍完整支撑时停车守盾，待原全源Danger许可且实际L释放，再以一次合法D＋Shift转移/反向制动；等待必须占原149帧，不另添80帧，不忽略Bell或其他威胁。
2. Main：第5次J在E24.2665刚满足双J费用门时打出，重置0.55秒等待；随后L耗到0。候选是明确延期这次攻击，继续原fallback防守，后续由freshDanger/完整反击窗口重新授权，不删必须的L、不改能量或900预算。留下Rust压力和改变位置都可能使候选失败。
3. 独立追踪pre/历史FATAL的对象出生与失败清理因果，不把本轮局部确定性释放当成全资源修复；之后仍须正式视听、正常显示/速度、目标硬件和用户试玩。

原十一项及全部未完成标准见[剩余总待办](2026-10-18-project-remaining-backlog-v001.md)。技术过程与逐次裁决见`G:/knight/Knight/.superpowers/sdd/2026-10-27-main-exit-reference-lifetime/progress.md`；不据本记录将目标标记complete、paused或blocked。
