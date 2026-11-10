# v259 · 下降区资源边界（进行中，未验收）

## 2026-11-06 · 本轮最终记录（完整序章仍未验收）

一审C0/I2/M0，两项收据完整性漏口均由root实际AST反例复现RED后，在**一次修补**中修正：Descent三流错流/镜像与guard-fault矛盾前缀/两流不一致。64＋88控制GREEN，当前全部纯控制497通过；审后原生三案同一句柄92156终态均PASS：合同2035ms、退出反例3986ms、普通程序集排除542ms。没有第二审查，不把一次TDD修补称为复审通过。

原24批仍20PASS/4严格FAIL，普通17仍15PASS/2FAIL；pre的6Handle、post的FATAL/profile_missing及REF pre FATAL继续OPEN，不因新增宿主通过或上游相似报告关闭。主线/探索自动路线虽已从标题经Boss至支撑出口/60hold/0恢复，但全76、真人操作、正常视听/硬件、正式美术连续动作/R-I/R-C/R-P与原十一项仍待完成，当前不能交付为稳定成品。

最终绑定30案150保护清单对、当前完整原生范围1898/938CS/515testCS的集合和SHA、180封存binary与当前6普通binary均0差异；额外169对冻结helper副本也0差异，最终64控制与REF封存一致。基本helper20项与冻结副本是不同口径；canonical4历史版、新warning控制3版，以及冻结boundary55/64版明确保留，不声称所有旧案都用了最终helper。代码C#运行epoch只有一ordinary/一REF binary版本。

原始证据及完整裁决：[最终审查与一次修补](../../.superpowers/sdd/2026-11-05-descent-resource-boundary-v259/final-review-v259.md)、[最终绑定](../../.superpowers/sdd/2026-11-05-descent-resource-boundary-v259/final-binding-v259.json)、[全部进度与决策](../../.superpowers/sdd/2026-11-05-descent-resource-boundary-v259/progress.md)。没有owned native/pure/reviewer job继续运行；下一步进入已定位的短喷→普通空中承载交接真实失败测试，不重新跑此批来筛绿。原目标保持ACTIVE，未缩减。

以下“当前／正在”段落为执行时的分阶段原始记录，最新状态以上节为准。

## 最新终态与审查状态

本轮24项整批已终态：20PASS/4严格FAIL，普通17为15PASS/2FAIL，REF pre原生FATAL和原guard-fault受控warning误拦也保留。原生故障仍未解决，不作为稳定版本或完整序章验收。主线/探索线均从标题经正常headless键盘驾驶到Boss、实体支撑Completed出口、60帧保持、0恢复；它们仍human/coverage/fullGoal=false，不是76全量或正式视听认可。

guard-fault宿主误拦已单独修正：精确原生source/function/三流数量与顺序/错误流伪造回执的实际80反控通过，最终新原生233441-f46b0f native3699ms严格PASS。原24结果不重写，未知警告和原生错误不豁免；其余六退出模式仍零warning要求。最终实际纯控制480通过。

最终审前绑定27案135保护对、当前完整native inventory1898/938CS/515testCS集合与SHA、162封存binary及当前6普通binary均无差异；ordinary/REF各一binary版本。**helper有已知历史版本差异**：canonical3版、新warning控制2版，当前版在最终案中，其他18helper无冲突；不宣称旧24使用最终helper。审计见workspace/audit-v259.json（15:43:59Z）。

唯一fresh审查 `/root/descent_resource_boundary_final_review_v259` 已派出，原地before/current8组与新合同、实际失败/修订/代价均提供；尚未返回，不提前声称审查通过。全部代码/host/资产冻结，无owned native/pure jobs；后续先处理本次唯一审查，再进入真实动作handoff失败测试。原十一项继续ACTIVE。

当前结论：新增检查尚不稳定，不能称资源问题已修复。原十一项目标继续 ACTIVE。普通源已恢复正确位置并 Rebuild，models666ms及加载程序集排除门668ms通过；完整普通17门和本轮最终审查尚未执行，不能沿用 v258 的结果给当前1898源签全套资格。

### 后续实际推进：场景退休竞态修复已局部验证，完整回归正在运行

上一段的两份普通通过属于**退休修复之前**，不签当前生产增量。221653-e286fc native17714ms在小合同完成后原生FATAL：未返回call104为CanyonSkitterSettings/serial34、原生身份未知；还记录独立AudioStreamPlayer已释放访问，调用链为延迟BeginPlayingAfterResidents→AbortResidentPreparation→ReturnToTitle→SetPaused。不能仅据两个错误靠近就说同因。

对后一条已完成真实失败测试：222653-93ef42 native4770ms，清理首await前退休状态为false，场景在清理过程中从Title进入Intro，并附原2000ms混音预算超时。最小修复提取GameFlow.BeginSceneRetirement，正式PrepareApplicationQuitAsync与自有测试根共同调用，音频QueueFree之前同步撤销后续启动权限。调用既有Level.BeginApplicationQuit/Audio.BeginApplicationQuit，不新增全局Quit/GC/cache/保活策略，原mix采样/60帧/2000ms/await位置不变。它是一个小的**生产生命周期修复**，不再属于仅诊断改动。

同命令修复后222934-92eba5 native2053ms严格PASS：即时退休/Title不重新启动、真实字段阶段、挂前主异常/根释放、挂后实际Run主异常和原finally均通过，drain15ms/38ms。只证明这条已重现的竞态修复，不因单次quiet关闭历史Canyon或其他原生FATAL。

当前唯一父句柄47897在运行一批冻结源回归：原pre REF一次、普通17门及6个应用退出控制，每种各一次、原预算不变；任何FAIL保留且不重试筛绿。REF后models首先恢复普通DLL，再验证不同普通场景。全部源/宿主/资产冻结，仅更新状态文档；本轮最终联合审查尚未派出。机器状态为workspace/current-regression-results-v259.json和各案native.wait/provenance，不从进程启动推断通过。

14:47:57Z检查点：12/24项已返回，10严格PASS、2严格FAIL；父句柄47897仍由write_stdin确认RUNNING，当前recycling-traps，不重启。普通models/state/patrol/geometry/allocation与5个退出控制通过。受控退出故障回调案engine0/3895ms、实际replacement保持和正常退出成功，但出现精确受控WARNING，宿主当前一律veto WARNING，因此仍记严格FAIL，不静默豁免；这不是证明应用行为失败，也不能直接改判PASS。

本次原pre REF223408-ae7386 native69336ms以0xC000001D退出，Supply index4返回、Descent index5挂前观察拒绝Definitions/index1无效，AddChild未到；无Handle ERROR。唯一未返回finalizer call783为PrologueSectorDefinition/serial1037/nativeID9223375430835242586，首次关联Supply旧Definitions/index4/sector4。无效index1尚未读取serial，不能和1037视作同一对象。journal7870连续行、未达8192，但成功guard未到达，不能证明fault/scopes完整；该FATAL继续OPEN。完整只读逐行定位已归入ledger。

当前实际纯控制373=55+51+26+157+84全部通过/native=false；不能替代原生验证。媒体检查14:47:57Z仍7,427,750B/86文件/Notify=false，没有新媒体或自动化。

后续普通pre224905-5dca76 native147863ms/engine0、44案例和原退出drain返回，仍因6条Handle ERROR严格FAIL，首栈在BasinSupply.OpenSample525（不是本次REF的Descent挂前拒绝）。post225215-043c2f native189665ms原生FATAL，在登记案例66的真实session准备前又遇到resident profile_missing，不能做相机/恢复断言；宿主先报scope不全不代表那是根因。原4条限定负控warning仍单列。二者均不重试筛绿。

已补[官方提交／公开报告只读核对](2026-11-05-godot-dotnet-upstream-readonly-v259.md)，区分源码重合、confirmed公开症状、已not_planned关闭的推测报告与本项目因果证据；没有引擎/GC/cache/保活改动，也不承诺升级即可解决。独立动作最小测试入口已定位，待当前冻结批次结束后才写测试，不以源码推测冒充修补完成。

## 已做和未做

- 仅测试/诊断宿主：在原下降测试挂载前后同步读取当前5个区域配置及玩家 Profile/Motor Tuning/Combat Tuning 的标量身份；挂载前用既有 owned-root 保护，挂载后位于原 try 内，仍走原音频 finally。未更改生产能力、场景、伤害、动作、美术、存档或难度。
- REF pre 成功出口新增 journal 实际完整性检查，不能把丢行/未闭合 scope 标为成功；本轮尚未重新运行原 pre，实际长程结果仍未知。
- 新独立合同真实缺少API的 RED 已见；收据实际AST纯控制55、既有准入51通过。一次正确实现的小合同通过，但后续同样正确源再次失败，故不计为稳定 GREEN。
- 独立合同首个成功fixture现在只在即时快照之后等待实际 Playing 再清理；原 pre 未增加等待。此差异关联启动生命周期，不证明 ApplySettings 或混音驱动是根因。
- 合同自有清理失败会保留原异常并尝试其余自有根，不再直接覆盖主异常。聚合双异常分支尚未独立证明；最新 assertion 增加实际异常类型/消息与节点有效性，尚未重新执行。

## 原始执行（全部保留，不重跑筛绿）

证据根 `G:/knight/验证证据/october13-stable-claw`，下表省略统一前缀；完整目录名见 workspace ledger。

| 原生案时间/尾标识 | 结果 |
|---|---|
| 211424-f404fd… | 构建失败：System/Godot Environment 歧义，不算行为RED |
| 211735-629f45… | native620ms，实际缺少3个API，行为RED |
| 212021-447fe2… | 构建失败：命名空间限定遗漏，不算行为RED |
| 213142-ef97ed…（临时负控） | native1843ms，树未进入+清理覆盖异常；未到预期断言 |
| 213813-018230…（临时负控） | native4546ms，首fixture即时清理音频超时；未到预期断言 |
| 214524-ea4b1a… / 215323-15a9bd… | 普通models547ms、下降真实玩法12333ms通过，下降drain5帧104ms |
| 220058-61ba42…（正确源） | native11329ms严格PASS；首fixture真实Title→Playing5帧后drain92ms，实际Run异常清理1848ms；仅一次成功，不代表稳定 |
| 220230-ef274c…（临时负控） | native9470ms失败；外层自有清理超时2108ms覆盖主异常，另有1 ObjectDB泄漏；不是已验证的精确负控RED |
| 220525-77ddec…（正确源） | native10950ms严格FAIL：第二fixture7个Handle ERROR及清理契约断言失败；首fixturestartup/drain通过，不冲销后续失败 |
| 220833-862306…（当前正确源普通恢复） | native666ms、exit0、无超时/guard；models通过，不等整套回归 |
| 221224-17ab1c…（当前普通排除门） | native668ms、exit0、无超时/guard；实际加载程序集的原lifetime/identity两类均不存在，不是新增合同第三类的独立反射证明 |

每次临时移动观察器均已在进程终态后恢复到原 try **内部**；目前源不是负控版本。所有本轮自有执行现已终态。最后两案10个保护清单对零差异；当前1898个清单内文件逐SHA与最新封存源零差异。此为局部绑定，不冒称本轮全部历史案同源、完整文件集额外项枚举或当前全17资格。

最新正确源失败不能直接归为“音频没混音”：只看到原 finally 摘要、没有第二drain成功标记；当时 afterActual 未输出，类型/消息未知。下一先读该案七条栈与同案标量关联，再利用新增异常信息做有界取证；不提高2000ms/60帧，不改GC/cache/KeepAlive或引擎，不运行原 pre 或全回归来掩盖无效契约。

只读核对已完成：七条都是 GCHandle.FromIntPtr→SwapGCHandleForType1284→Node.AddChild848→Descent.Run601，经合同Run93触发；不是Supply或Dispose栈。第二root283065189894/world283769834005的before字段与首fixture共享对应serial/native身份，但错误栈没有对象ID，不能绑定其中任一字段。全211次ENTER/RETURN配对、七错误时开放已跟踪Dispose均0。定向故障writer会抑制after快照，故缺after行不证明观察器未进入。实际 afterActual/root有效性仍待新信息，不用近邻Dispose推定因果。

## 用户美术选择与边界

U-20261105-01 已记录：V1批准身体/披风原图暂找不到，停止扩大搜索，后续重新审阅现有候选，不转授旧批准、不自动付费重绘。当前程序身体/披风与正式分层动画有差距；连续动作、设计感、惯性/角色特色/物理手感 R-I/R-C/R-P 未完成。误给的三段视频仍作废。

只读素材接线见 [v258素材事实表](../art/player/2026-11-05-current-motion-asset-binding-readonly-v258.md)。仅在聊天展示已保存候选图的询问尚待答复，不重复催问或当成授权。

本轮已追加[动作承载交接只读核对](../art/player/2026-11-05-motion-transition-continuity-readonly-v259.md)：具体定位同状态短喷→普通空中模板撤销、整rig一拍回身、落地支撑/phase重建三处潜在突变。第一点优先作为下一实际动作修补的原生失败测试，不把源码推理当视觉复现或动作验收；没有改角色物理/美术/动画代码。

## 本轮裁决与代价（截至当前，非最终审查）

1. 在现有脏树原地有界续制、保留before、不动Git；代价是隔离程度有限。
2. 挂前自有根保护、挂后原finally；代价是新增固定字段读取/日志会扰动诊断时序，不声称零影响。
3. 使用专职新合同CS，而非继续堆入旧大runner；代价是源集合1898，需当前全套重新资格验证。
4. 加实际Run异常路径，而非仅凭源码位置判清理正确；代价是额外自有fixture和执行复杂度，55秒预算不变。
5. 仅首个成功fixture快照后等待真实Playing；代价是独立合同时间/冷暖上下文改变，不证明精确音频因果，原pre等待不变。
6. 合同外层聚合原始异常和清理异常、继续其他自有清理；代价是清理也失败时异常形式变化，该双失败分支仍需实测。
7. 停止重复选绿，保留本轮未完成状态、先恢复普通构建；代价是原pre/full17/唯一新审查仍欠，不能把局部通过包装成完成。
8. 修订诊断-only边界，先修已重现的场景退休竞态：共用正式退出的同步退休入口，不增加原mix等待；代价是生产GameFlow与测试清理均改变，须当前完整回归和审查，不能归为原生FATAL已修。
9. 一次冻结源序列运行REF pre、普通17及6退出控制；代价是更长的有界无窗口验证，不重建自动化、不靠重试筛绿。
10. 在冻结回归期间只读定位动作交接测试入口，未动渲染代码；代价是这些是待实测假设，不能算动作变美或修复。
11. guard-fault采用原生raw精确来源/顺序，而不插入C#logger改变编译epoch；代价是依赖固定日志格式，未证明独立typed logger scope，镜像/前缀漏口已由本次唯一审查后一次TDD补足。
12. 旧Handle/FATAL/profile_missing保持交付阻断，不由相邻serial或上游标题判因；代价是稳定性仍需独立调查。
13. headless不认证正常显示/硬件真人/动作美术，维持当前许可；代价是仍需后续实际验收与可能返工。
14. 有限路线不等原十一项/76全量，不缩减范围；代价是仍有内容、覆盖和正式素材工作。
15. 使用root实际审计，不说审查者独立全量复算；代价是审计工具仍有自检残余风险，额外冻结helper副本本轮已实际核对。
16. 不推定引擎/GC/cache/KeepAlive/真实存档实验权限；代价是这些候选对照暂未执行，不能反推它们安全或有效。

完整过程：[ledger](../../.superpowers/sdd/2026-11-05-descent-resource-boundary-v259/progress.md)、[设计](../superpowers/specs/2026-11-05-descent-resource-boundary-v259-design.md)、[计划](../superpowers/plans/2026-11-05-descent-resource-boundary-v259.md)。本轮没有最终审查或 Minor 新结论，不替代 v258 审查。没有GUI/系统输入/付费/新媒体/额外dump/真实存档实验/自动化/Git写/发布/删除；容量检查2026-11-05T14:09:51Z为7,427,750B、86文件、Notify=false。
